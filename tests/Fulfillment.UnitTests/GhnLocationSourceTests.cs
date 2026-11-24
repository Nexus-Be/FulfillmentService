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
}
