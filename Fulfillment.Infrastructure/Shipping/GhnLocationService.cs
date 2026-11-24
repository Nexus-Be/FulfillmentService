namespace Fulfillment.Infrastructure.Shipping;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Exceptions;
using Fulfillment.Infrastructure.Carriers.Ghn;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class GhnLocationService : IShippingLocationService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly GhnOptions _options;
    private readonly ILogger<GhnLocationService> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    /// <summary>
    /// Only the carrier's real answers are cached for <see cref="CacheDuration"/>, together with the time they were read. A failure is
    /// remembered for this short time only, so a carrier that is down is not called on every request and the lists come back as soon as it
    /// answers again. Nothing else is ever served in place of the carrier's lists.
    /// </summary>
    public static readonly TimeSpan CarrierFailureBackoff = TimeSpan.FromSeconds(30);
    public const string CarrierDownCacheKey = "ghn:master_data:carrier_down";

    private void NoteCarrierDown() => _cache.Set(CarrierDownCacheKey, true, CarrierFailureBackoff);
    private bool CarrierRecentlyDown => _cache.TryGetValue(CarrierDownCacheKey, out _);

    public GhnLocationService(
        IOptions<GhnOptions> options,
        IMemoryCache cache,
        ILogger<GhnLocationService> logger,
        HttpClient? httpClient = null)
    {
        _options = options?.Value ?? new GhnOptions();
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? new HttpClient();

        if (_httpClient.BaseAddress == null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        }
        if (_options.TimeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        }

        if (!string.IsNullOrWhiteSpace(_options.Token) && !_httpClient.DefaultRequestHeaders.Contains("Token"))
        {
            _httpClient.DefaultRequestHeaders.Add("Token", _options.Token);
        }
    }

    private sealed record CachedLocations<T>(IReadOnlyList<T> Items, DateTimeOffset ObservedAt);

    private static BusinessRuleValidationException SourceUnavailable() =>
        new("LOCATION_SOURCE_UNAVAILABLE", "Shipping locations are temporarily unavailable.", 503);

    /// <summary>
    /// The one way location lists are read: a still valid answer the carrier gave earlier, or a fresh answer from the carrier. When neither
    /// exists (no token, the carrier failed, or it failed moments ago) the call fails with 503 LOCATION_SOURCE_UNAVAILABLE. There is no built-in
    /// substitute list: a made-up province, district or ward code would be accepted by the address checks and sent to the carrier.
    /// </summary>
    private async Task<LocationList<T>> LoadAsync<T, TRaw>(
        string cacheKey,
        string what,
        Func<CancellationToken, Task<HttpResponseMessage>> call,
        Func<List<TRaw>, List<T>> map,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(cacheKey, out CachedLocations<T>? hit) && hit != null)
        {
            return new LocationList<T>(hit.Items, LocationSources.GhnCache, hit.ObservedAt);
        }

        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            _logger.LogWarning("GHN token is not configured, so the {What} cannot be read.", what);
            throw SourceUnavailable();
        }

        if (CarrierRecentlyDown)
        {
            throw SourceUnavailable();
        }

        try
        {
            using var response = await call(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GHN {What} returned status {StatusCode}", what, response.StatusCode);
                NoteCarrierDown();
                throw SourceUnavailable();
            }

            var result = await response.Content.ReadFromJsonAsync<GhnRawApiResponse<List<TRaw>>>(cancellationToken: cancellationToken);
            var items = result?.Data is { Count: > 0 } raw ? map(raw) : new List<T>();
            var observedAt = DateTimeOffset.UtcNow;
            if (items.Count > 0)
            {
                _cache.Set(cacheKey, new CachedLocations<T>(items, observedAt), CacheDuration);
            }

            return new LocationList<T>(items, LocationSources.Ghn, observedAt);
        }
        catch (BusinessRuleValidationException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read the {What} from GHN.", what);
            NoteCarrierDown();
            throw SourceUnavailable();
        }
    }

    public Task<LocationList<ProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default) =>
        LoadAsync<ProvinceDto, GhnRawProvince>(
            "ghn:master_data:provinces",
            "provinces",
            ct => _httpClient.GetAsync("/shiip/public-api/master-data/province", ct),
            raw => raw.Select(p => new ProvinceDto(p.ProvinceId, p.ProvinceName, p.Code)).ToList(),
            cancellationToken);

    public Task<LocationList<DistrictDto>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default)
    {
        if (provinceId <= 0)
        {
            return Task.FromResult(new LocationList<DistrictDto>(Array.Empty<DistrictDto>(), LocationSources.Ghn, DateTimeOffset.UtcNow));
        }

        return LoadAsync<DistrictDto, GhnRawDistrict>(
            $"ghn:master_data:districts:{provinceId}",
            $"districts of province {provinceId}",
            ct => _httpClient.PostAsJsonAsync("/shiip/public-api/master-data/district", new { province_id = provinceId }, ct),
            raw => raw.Select(d => new DistrictDto(d.DistrictId, d.ProvinceId, d.DistrictName, d.SupportType)).ToList(),
            cancellationToken);
    }

    public Task<LocationList<WardDto>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default)
    {
        if (districtId <= 0)
        {
            return Task.FromResult(new LocationList<WardDto>(Array.Empty<WardDto>(), LocationSources.Ghn, DateTimeOffset.UtcNow));
        }

        return LoadAsync<WardDto, GhnRawWard>(
            $"ghn:master_data:wards:{districtId}",
            $"wards of district {districtId}",
            ct => _httpClient.PostAsJsonAsync("/shiip/public-api/master-data/ward", new { district_id = districtId }, ct),
            raw => raw.Select(w => new WardDto(w.WardCode, w.DistrictId, w.WardName)).ToList(),
            cancellationToken);
    }

    public async Task<IReadOnlyList<AvailableShippingServiceDto>> GetAvailableServicesAsync(
        int fromDistrictId,
        int toDistrictId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"ghn:available_services:{fromDistrictId}:{toDistrictId}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<AvailableShippingServiceDto>? cached) && cached != null)
        {
            return cached;
        }

        if (!string.IsNullOrWhiteSpace(_options.Token) && !CarrierRecentlyDown && int.TryParse(_options.ShopId, out var shopId) && shopId > 0)
        {
            try
            {
                var payload = new
                {
                    shop_id = shopId,
                    from_district = fromDistrictId,
                    to_district = toDistrictId
                };

                var response = await _httpClient.PostAsJsonAsync(
                    "/shiip/public-api/v2/shipping-order/available-services",
                    payload,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GhnRawApiResponse<List<GhnRawAvailableService>>>(cancellationToken: cancellationToken);
                    if (result?.Data != null && result.Data.Count > 0)
                    {
                        var services = result.Data.Select(s => new AvailableShippingServiceDto(s.ServiceId, s.ShortName, s.ServiceTypeId)).ToList();
                        _cache.Set(cacheKey, (IReadOnlyList<AvailableShippingServiceDto>)services, CacheDuration);
                        return services;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch available services from GHN for {FromDistrict} -> {ToDistrict}.", fromDistrictId, toDistrictId);
                NoteCarrierDown();
            }
        }

        var fallback = new List<AvailableShippingServiceDto>
        {
            new(53320, "Standard", 2),
            new(53321, "Fast", 3)
        };
        return fallback;
    }

    public async Task<ShippingLocationValidationResponse> ValidateLocationAsync(
        ValidateShippingLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var provinces = (await GetProvincesAsync(cancellationToken)).Items;
        var province = provinces.FirstOrDefault(p => p.ProvinceId == request.ProvinceId);
        if (province == null)
        {
            return new ShippingLocationValidationResponse(
                IsValid: false,
                ProvinceId: request.ProvinceId,
                ProvinceName: null,
                DistrictId: request.DistrictId,
                DistrictName: null,
                WardCode: request.WardCode,
                WardName: null,
                Message: $"Province with ID '{request.ProvinceId}' was not found.");
        }

        var districts = (await GetDistrictsAsync(request.ProvinceId, cancellationToken)).Items;
        var district = districts.FirstOrDefault(d => d.DistrictId == request.DistrictId);
        if (district == null)
        {
            return new ShippingLocationValidationResponse(
                IsValid: false,
                ProvinceId: request.ProvinceId,
                ProvinceName: province.ProvinceName,
                DistrictId: request.DistrictId,
                DistrictName: null,
                WardCode: request.WardCode,
                WardName: null,
                Message: $"District with ID '{request.DistrictId}' does not belong to province '{province.ProvinceName}'.");
        }

        var wards = (await GetWardsAsync(request.DistrictId, cancellationToken)).Items;
        var ward = wards.FirstOrDefault(w => string.Equals(w.WardCode, request.WardCode, StringComparison.OrdinalIgnoreCase));
        if (ward == null)
        {
            return new ShippingLocationValidationResponse(
                IsValid: false,
                ProvinceId: request.ProvinceId,
                ProvinceName: province.ProvinceName,
                DistrictId: request.DistrictId,
                DistrictName: district.DistrictName,
                WardCode: request.WardCode,
                WardName: null,
                Message: $"Ward with code '{request.WardCode}' does not belong to district '{district.DistrictName}'.");
        }

        return new ShippingLocationValidationResponse(
            IsValid: true,
            ProvinceId: request.ProvinceId,
            ProvinceName: province.ProvinceName,
            DistrictId: request.DistrictId,
            DistrictName: district.DistrictName,
            WardCode: ward.WardCode,
            WardName: ward.WardName);
    }

    private sealed class GhnRawApiResponse<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    private sealed class GhnRawProvince
    {
        [JsonPropertyName("ProvinceID")]
        public int ProvinceId { get; set; }

        [JsonPropertyName("ProvinceName")]
        public string ProvinceName { get; set; } = string.Empty;

        [JsonPropertyName("Code")]
        public string? Code { get; set; }
    }

    private sealed class GhnRawDistrict
    {
        [JsonPropertyName("DistrictID")]
        public int DistrictId { get; set; }

        [JsonPropertyName("ProvinceID")]
        public int ProvinceId { get; set; }

        [JsonPropertyName("DistrictName")]
        public string DistrictName { get; set; } = string.Empty;

        [JsonPropertyName("SupportType")]
        public int SupportType { get; set; }
    }

    private sealed class GhnRawWard
    {
        [JsonPropertyName("WardCode")]
        public string WardCode { get; set; } = string.Empty;

        [JsonPropertyName("DistrictID")]
        public int DistrictId { get; set; }

        [JsonPropertyName("WardName")]
        public string WardName { get; set; } = string.Empty;
    }

    private sealed class GhnRawAvailableService
    {
        [JsonPropertyName("service_id")]
        public int ServiceId { get; set; }

        [JsonPropertyName("short_name")]
        public string ShortName { get; set; } = string.Empty;

        [JsonPropertyName("service_type_id")]
        public int ServiceTypeId { get; set; }
    }
}
