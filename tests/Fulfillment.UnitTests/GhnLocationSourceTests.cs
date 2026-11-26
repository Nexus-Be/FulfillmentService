namespace Fulfillment.UnitTests;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Exceptions;
using Fulfillment.Infrastructure.Carriers.Ghn;
using Fulfillment.Infrastructure.Shipping;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>
/// Location lists come only from the carrier (or from an answer it gave earlier that is still cached). When neither exists the call fails with
/// 503 LOCATION_SOURCE_UNAVAILABLE: there is no built-in substitute list, and an address is never judged wrong because the source was down.
/// </summary>
public class GhnLocationSourceTests
{
    private const string ProvincesKey = "ghn:master_data:provinces";

    private sealed class Carrier(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        private int _calls;
        public int Calls => _calls;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var index = Math.Min(Interlocked.Increment(ref _calls) - 1, statuses.Length - 1);
            var status = statuses[index];
            if (status != HttpStatusCode.OK) return new HttpResponseMessage(status) { Content = new StringContent("{}") };

            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            string data;
            if (path.EndsWith("/province")) data = "[{\"ProvinceID\":202,\"ProvinceName\":\"Ho Chi Minh\",\"Code\":\"79\"},{\"ProvinceID\":201,\"ProvinceName\":\"Ha Noi\",\"Code\":\"01\"}]";
            else if (path.EndsWith("/district") && body.Contains("202")) data = "[{\"DistrictID\":1442,\"ProvinceID\":202,\"DistrictName\":\"Quan 1\",\"SupportType\":3},{\"DistrictID\":1446,\"ProvinceID\":202,\"DistrictName\":\"Quan 4\",\"SupportType\":3}]";
            else if (path.EndsWith("/district")) data = "[{\"DistrictID\":1485,\"ProvinceID\":201,\"DistrictName\":\"Cau Giay\",\"SupportType\":3}]";
            else if (path.EndsWith("/ward") && body.Contains("1442")) data = "[{\"WardCode\":\"20101\",\"DistrictID\":1442,\"WardName\":\"Phuong Ben Nghe\"}]";
            else data = "[]";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"code\":200,\"message\":\"Success\",\"data\":" + data + "}") };
        }
    }

    private sealed class FixedBody(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    private static GhnLocationService Create(string token, IMemoryCache cache, HttpMessageHandler handler) =>
        new(Options.Create(new GhnOptions { Token = token, BaseUrl = "https://dev-online-gateway.ghn.vn" }), cache,
            NullLogger<GhnLocationService>.Instance, new HttpClient(handler) { BaseAddress = new Uri("https://dev-online-gateway.ghn.vn/") });

    private static async Task<BusinessRuleValidationException> Unavailable(Func<Task> act) =>
        (await act.Should().ThrowAsync<BusinessRuleValidationException>()).Which;

    [Fact]
    public async Task Without_a_token_the_list_is_unavailable_and_the_carrier_is_not_called()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var carrier = new Carrier(HttpStatusCode.OK);
        var service = Create(token: "", cache, carrier);

        var ex = await Unavailable(() => service.GetProvincesAsync());

        ex.ErrorCode.Should().Be("LOCATION_SOURCE_UNAVAILABLE");
        ex.StatusCode.Should().Be(503);
        carrier.Calls.Should().Be(0);
        cache.TryGetValue(ProvincesKey, out _).Should().BeFalse();
    }

    [Fact]
    public async Task A_carrier_failure_is_503_never_a_sample_and_the_list_returns_when_the_carrier_answers()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var carrier = new Carrier(HttpStatusCode.InternalServerError, HttpStatusCode.OK);
        var service = Create(token: "token", cache, carrier);

        (await Unavailable(() => service.GetProvincesAsync())).StatusCode.Should().Be(503);
        cache.TryGetValue(ProvincesKey, out _).Should().BeFalse("a failure is never cached as a list");

        // While the carrier is known to be down it is not called again.
        (await Unavailable(() => service.GetProvincesAsync())).ErrorCode.Should().Be("LOCATION_SOURCE_UNAVAILABLE");
        carrier.Calls.Should().Be(1);

        cache.Remove(GhnLocationService.CarrierDownCacheKey);
        var recovered = await service.GetProvincesAsync();
        recovered.Source.Should().Be(LocationSources.Ghn);
        recovered.Items.Should().HaveCount(2);
        recovered.Items.Should().Contain(p => p.ProvinceId == 202 && p.ProvinceName == "Ho Chi Minh");
    }

    [Fact]
    public async Task A_cached_answer_keeps_the_time_the_carrier_gave_it_and_is_reported_as_cached()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var carrier = new Carrier(HttpStatusCode.OK);
        var service = Create(token: "token", cache, carrier);

        var first = await service.GetProvincesAsync();
        var second = await service.GetProvincesAsync();

        first.Source.Should().Be(LocationSources.Ghn);
        second.Source.Should().Be(LocationSources.GhnCache);
        second.ObservedAt.Should().Be(first.ObservedAt);
        carrier.Calls.Should().Be(1);
    }

    [Fact]
    public async Task A_cached_list_is_still_served_while_the_carrier_is_down()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var carrier = new Carrier(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        var service = Create(token: "token", cache, carrier);
        await service.GetProvincesAsync();
        await Unavailable(() => service.GetDistrictsAsync(202).ContinueWith(t => t.Result));   // districts were never cached and the carrier answers 500

        var provinces = await service.GetProvincesAsync();

        provinces.Source.Should().Be(LocationSources.GhnCache);
    }

    [Fact]
    public async Task Validate_accepts_a_real_hierarchy_and_names_it_from_the_carrier_lists()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = Create(token: "token", cache, new Carrier(HttpStatusCode.OK));

        var res = await service.ValidateLocationAsync(new ValidateShippingLocationRequest(202, 1442, "20101"));

        res.IsValid.Should().BeTrue();
        res.DistrictName.Should().Be("Quan 1");
        res.WardName.Should().Be("Phuong Ben Nghe");
    }

    [Fact]
    public async Task Validate_rejects_a_district_of_another_province_and_an_invented_ward_only_from_real_lists()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = Create(token: "token", cache, new Carrier(HttpStatusCode.OK));

        var wrongProvince = await service.ValidateLocationAsync(new ValidateShippingLocationRequest(201, 1442, "20101"));
        wrongProvince.IsValid.Should().BeFalse();
        wrongProvince.Message.Should().Contain("does not belong to province");

        var invented = await service.ValidateLocationAsync(new ValidateShippingLocationRequest(202, 1442, "144201"));
        invented.IsValid.Should().BeFalse();
        invented.Message.Should().Contain("does not belong to district");
    }

    [Fact]
    public async Task Validate_with_the_source_down_is_503_not_an_invalid_address()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = Create(token: "token", cache, new Carrier(HttpStatusCode.ServiceUnavailable));

        var ex = await Unavailable(() => service.ValidateLocationAsync(new ValidateShippingLocationRequest(202, 1442, "20101")));

        ex.ErrorCode.Should().Be("LOCATION_SOURCE_UNAVAILABLE");
        ex.StatusCode.Should().Be(503);
    }

    [Theory]
    [InlineData("{\"code\":400,\"message\":\"Bad request\",\"data\":null}")]
    [InlineData("{\"code\":200,\"message\":\"Success\",\"data\":null}")]
    [InlineData("{\"message\":\"no code\"}")]
    [InlineData("not json at all")]
    [InlineData("{\"code\":200,\"message\":\"Success\",\"data\":[]}")]
    public async Task An_http_200_without_a_usable_list_is_a_failed_read_for_provinces(string body)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = Create(token: "token", cache, new FixedBody(body));

        var ex = await Unavailable(() => service.GetProvincesAsync());

        ex.ErrorCode.Should().Be("LOCATION_SOURCE_UNAVAILABLE");
        cache.TryGetValue(ProvincesKey, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("{\"code\":400,\"message\":\"Bad request\",\"data\":null}")]
    [InlineData("{\"code\":200,\"message\":\"Success\",\"data\":null}")]
    public async Task A_district_or_ward_read_with_an_error_body_or_no_list_is_503_not_an_empty_answer(string body)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = Create(token: "token", cache, new FixedBody(body));

        (await Unavailable(() => service.GetDistrictsAsync(202))).StatusCode.Should().Be(503);
        cache.Remove(GhnLocationService.CarrierDownCacheKey);
        (await Unavailable(() => service.GetWardsAsync(1442))).StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task A_ward_list_that_GHN_confirms_is_empty_is_an_answer_and_is_not_cached()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = Create(token: "token", cache, new FixedBody("{\"code\":200,\"message\":\"Success\",\"data\":[]}"));

        var wards = await service.GetWardsAsync(1442);

        wards.Items.Should().BeEmpty();
        wards.Source.Should().Be(LocationSources.Ghn);
        cache.TryGetValue("ghn:master_data:wards:1442", out _).Should().BeFalse("an empty list is not kept for 24 hours");
    }

    [Fact]
    public async Task An_invalid_province_rejection_does_not_trip_carrier_down_for_valid_provinces()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new InvalidProvinceThenValidHandler();
        var service = Create(token: "token", cache, handler);

        await Unavailable(() => service.GetDistrictsAsync(2002));
        cache.TryGetValue(GhnLocationService.CarrierDownCacheKey, out _).Should().BeFalse();

        var valid = await service.GetDistrictsAsync(202);
        valid.Items.Should().ContainSingle(d => d.DistrictId == 1442);
    }

    private sealed class InvalidProvinceThenValidHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            if (body.Contains("2002"))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"code\":400,\"message\":\"province_id is invalid\",\"data\":null}")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"code\":200,\"message\":\"Success\",\"data\":[{\"DistrictID\":1442,\"ProvinceID\":202,\"DistrictName\":\"Quan 1\",\"SupportType\":3}]}")
            };
        }
    }

    // ---- the carrier's service list: no made-up services
    private static GhnLocationService CreateWithShop(string token, string shop, IMemoryCache cache, HttpMessageHandler handler) =>
        new(Options.Create(new GhnOptions { Token = token, ShopId = shop, BaseUrl = "https://dev-online-gateway.ghn.vn" }), cache,
            NullLogger<GhnLocationService>.Instance, new HttpClient(handler) { BaseAddress = new Uri("https://dev-online-gateway.ghn.vn/") });

    [Fact]
    public async Task The_service_list_is_the_carriers_own_and_is_cached()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateWithShop("token", "241115", cache, new FixedBody("{\"code\":200,\"message\":\"Success\",\"data\":[{\"service_id\":53322,\"short_name\":\"Hang nhe\",\"service_type_id\":2}]}"));

        var first = await service.GetAvailableServicesAsync(1442, 1820);
        var second = await service.GetAvailableServicesAsync(1442, 1820);

        first.Should().ContainSingle().Which.ServiceId.Should().Be(53322);
        second.Should().ContainSingle();
    }

    [Theory]
    [InlineData("", "241115", "{}")]
    [InlineData("token", "", "{}")]
    [InlineData("token", "abc", "{}")]
    [InlineData("token", "241115", "{\"code\":400,\"message\":\"x\",\"data\":null}")]
    [InlineData("token", "241115", "{\"code\":200,\"message\":\"Success\",\"data\":[]}")]
    [InlineData("token", "241115", "{\"code\":200,\"message\":\"Success\",\"data\":null}")]
    [InlineData("token", "241115", "not json")]
    public async Task Without_a_token_or_shop_or_a_successful_list_the_service_list_is_503_never_a_made_up_one(string token, string shop, string body)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateWithShop(token, shop, cache, new FixedBody(body));

        var ex = await Unavailable(() => service.GetAvailableServicesAsync(1442, 1820));

        ex.ErrorCode.Should().Be("LOCATION_SOURCE_UNAVAILABLE");
        ex.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task A_failed_service_list_read_is_503_and_nothing_is_cached()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateWithShop("token", "241115", cache, new FixedBody("{}", HttpStatusCode.InternalServerError));

        (await Unavailable(() => service.GetAvailableServicesAsync(1442, 1820))).StatusCode.Should().Be(503);
        cache.TryGetValue("ghn:available_services:1442:1820", out _).Should().BeFalse();
    }
}
