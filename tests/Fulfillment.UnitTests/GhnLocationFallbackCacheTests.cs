namespace Fulfillment.UnitTests;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Infrastructure.Carriers.Ghn;
using Fulfillment.Infrastructure.Shipping;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>
/// A9: the built-in sample locations (3 provinces with ids 201-203, which are not GHN's ids) are served when the carrier cannot be used, but
/// they are never cached for 24 hours: a missing token or one transient failure must not hide the real locations afterwards.
/// </summary>
public class GhnLocationFallbackCacheTests
{
    private const string ProvincesKey = "ghn:master_data:provinces";

    private sealed class ScriptedCarrier(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        private int _calls;
        public int Calls => _calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var index = Math.Min(Interlocked.Increment(ref _calls) - 1, statuses.Length - 1);
            var status = statuses[index];
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(status == HttpStatusCode.OK
                    ? "{\"code\":200,\"message\":\"Success\",\"data\":[{\"ProvinceID\":1,\"ProvinceName\":\"Real province\",\"Code\":\"01\"}]}"
                    : "{}")
            });
        }
    }

    private static GhnLocationService Create(string token, IMemoryCache cache, HttpMessageHandler handler) =>
        new(Options.Create(new GhnOptions { Token = token, BaseUrl = "https://dev-online-gateway.ghn.vn" }), cache,
            NullLogger<GhnLocationService>.Instance, new HttpClient(handler) { BaseAddress = new Uri("https://dev-online-gateway.ghn.vn/") });

    [Fact]
    public async Task Without_a_token_the_sample_is_served_but_not_cached()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var carrier = new ScriptedCarrier(HttpStatusCode.OK);
        var service = Create(token: "", cache, carrier);

        var provinces = await service.GetProvincesAsync();

        provinces.Should().HaveCount(3);
        provinces.Should().OnlyContain(p => p.ProvinceId >= 201 && p.ProvinceId <= 203);
        cache.TryGetValue(ProvincesKey, out _).Should().BeFalse("the sample must not be cached for 24 hours");
        carrier.Calls.Should().Be(0, "there is no token, so the carrier is not called");
    }

    [Fact]
    public async Task One_failure_serves_the_sample_briefly_then_the_real_locations_come_back_and_are_cached()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var carrier = new ScriptedCarrier(HttpStatusCode.InternalServerError, HttpStatusCode.OK);
        var service = Create(token: "token", cache, carrier);

        var first = await service.GetProvincesAsync();
        first.Should().HaveCount(3);
        cache.TryGetValue(ProvincesKey, out _).Should().BeFalse();

        // While the carrier is known to be down it is not called again, and the sample is still served.
        var during = await service.GetProvincesAsync();
        during.Should().HaveCount(3);
        carrier.Calls.Should().Be(1);

        // The back-off ends: the next call asks the carrier again and gets the real list, which is cached.
        cache.Remove(GhnLocationService.CarrierDownCacheKey);
        var recovered = await service.GetProvincesAsync();
        recovered.Should().ContainSingle().Which.ProvinceName.Should().Be("Real province");
        cache.TryGetValue(ProvincesKey, out _).Should().BeTrue();

        await service.GetProvincesAsync();
        carrier.Calls.Should().Be(2, "the real answer is cached, so the carrier is not asked a third time");
    }

    [Fact]
    public async Task A_real_answer_is_cached_for_a_long_time()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var carrier = new ScriptedCarrier(HttpStatusCode.OK);
        var service = Create(token: "token", cache, carrier);

        await service.GetProvincesAsync();
        await service.GetProvincesAsync();

        carrier.Calls.Should().Be(1);
    }
}
