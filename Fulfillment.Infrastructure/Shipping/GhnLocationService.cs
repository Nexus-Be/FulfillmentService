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
    /// Only the carrier's real answers are cached for <see cref="CacheDuration"/>. When the carrier cannot be used (no token) or just failed,
    /// the built-in sample locations are served WITHOUT being cached, and a failure is remembered for this short time only so a carrier that is
    /// down is not called on every request. Caching the sample for 24 hours made a single transient failure (or a missing token) hide the
    /// real locations until the cache expired or the service restarted.
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

    public async Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "ghn:master_data:provinces";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ProvinceDto>? cached) && cached != null)
        {
            return cached;
        }

        if (!string.IsNullOrWhiteSpace(_options.Token) && !CarrierRecentlyDown)
        {
            try
            {
                var response = await _httpClient.GetAsync("/shiip/public-api/master-data/province", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GhnRawApiResponse<List<GhnRawProvince>>>(cancellationToken: cancellationToken);
                    if (result?.Data != null && result.Data.Count > 0)
                    {
                        var provinces = result.Data.Select(p => new ProvinceDto(p.ProvinceId, p.ProvinceName, p.Code)).ToList();
                        _cache.Set(cacheKey, (IReadOnlyList<ProvinceDto>)provinces, CacheDuration);
                        return provinces;
                    }
                }
                else
                {
                    _logger.LogWarning("GHN GetProvinces returned status {StatusCode}", response.StatusCode);
                    NoteCarrierDown();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch provinces from GHN, falling back to local master data.");
                NoteCarrierDown();
            }
        }

        var fallback = GetFallbackProvinces();
        return fallback;
    }

    public async Task<IReadOnlyList<DistrictDto>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default)
    {
        if (provinceId <= 0)
        {
            return Array.Empty<DistrictDto>();
        }

        var cacheKey = $"ghn:master_data:districts:{provinceId}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<DistrictDto>? cached) && cached != null)
        {
            return cached;
        }

        if (!string.IsNullOrWhiteSpace(_options.Token) && !CarrierRecentlyDown)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "/shiip/public-api/master-data/district",
                    new { province_id = provinceId },
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GhnRawApiResponse<List<GhnRawDistrict>>>(cancellationToken: cancellationToken);
                    if (result?.Data != null && result.Data.Count > 0)
                    {
                        var districts = result.Data.Select(d => new DistrictDto(d.DistrictId, d.ProvinceId, d.DistrictName, d.SupportType)).ToList();
                        _cache.Set(cacheKey, (IReadOnlyList<DistrictDto>)districts, CacheDuration);
                        return districts;
                    }
                }
                else
                {
                    _logger.LogWarning("GHN GetDistricts returned status {StatusCode} for province {ProvinceId}", response.StatusCode, provinceId);
                    NoteCarrierDown();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch districts from GHN for province {ProvinceId}, falling back to local master data.", provinceId);
                NoteCarrierDown();
            }
        }

        var fallback = GetFallbackDistricts(provinceId);
        return fallback;
    }

    public async Task<IReadOnlyList<WardDto>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default)
    {
        if (districtId <= 0)
        {
            return Array.Empty<WardDto>();
        }

        var cacheKey = $"ghn:master_data:wards:{districtId}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<WardDto>? cached) && cached != null)
        {
            return cached;
        }

        if (!string.IsNullOrWhiteSpace(_options.Token) && !CarrierRecentlyDown)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "/shiip/public-api/master-data/ward",
                    new { district_id = districtId },
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GhnRawApiResponse<List<GhnRawWard>>>(cancellationToken: cancellationToken);
                    if (result?.Data != null && result.Data.Count > 0)
                    {
                        var wards = result.Data.Select(w => new WardDto(w.WardCode, w.DistrictId, w.WardName)).ToList();
                        _cache.Set(cacheKey, (IReadOnlyList<WardDto>)wards, CacheDuration);
                        return wards;
                    }
                }
                else
                {
                    _logger.LogWarning("GHN GetWards returned status {StatusCode} for district {DistrictId}", response.StatusCode, districtId);
                    NoteCarrierDown();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch wards from GHN for district {DistrictId}, falling back to local master data.", districtId);
                NoteCarrierDown();
            }
        }

        var fallback = GetFallbackWards(districtId);
        return fallback;
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

        var provinces = await GetProvincesAsync(cancellationToken);
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

        var districts = await GetDistrictsAsync(request.ProvinceId, cancellationToken);
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

        var wards = await GetWardsAsync(request.DistrictId, cancellationToken);
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

    private static IReadOnlyList<ProvinceDto> GetFallbackProvinces() => new List<ProvinceDto>
    {
        new(201, "Hà Nội", "01"),
        new(202, "Hồ Chí Minh", "79"),
        new(203, "Đà Nẵng", "48")
    };

    private static IReadOnlyList<DistrictDto> GetFallbackDistricts(int provinceId) => provinceId switch
    {
        201 => new List<DistrictDto>
        {
            new(1442, 201, "Quận Ba Đình", 3),
            new(1443, 201, "Quận Hoàn Kiếm", 3),
            new(1444, 201, "Quận Đống Đa", 3),
            new(1445, 201, "Quận Cầu Giấy", 3)
        },
        202 => new List<DistrictDto>
        {
            new(1446, 202, "Quận 1", 3),
            new(1447, 202, "Quận 3", 3),
            new(1448, 202, "Quận Bình Thạnh", 3)
        },
        203 => new List<DistrictDto>
        {
            new(1449, 203, "Quận Hải Châu", 3),
            new(1450, 203, "Quận Thanh Khê", 3)
        },
        _ => new List<DistrictDto>()
    };

    private static IReadOnlyList<WardDto> GetFallbackWards(int districtId) => districtId switch
    {
        1442 => new List<WardDto>
        {
            new("20101", 1442, "Phường Phúc Xá"),
            new("20102", 1442, "Phường Trúc Bạch"),
            new("20103", 1442, "Phường Vĩnh Phúc")
        },
        1444 => new List<WardDto>
        {
            new("20104", 1444, "Phường Cát Linh"),
            new("20105", 1444, "Phường Văn Miếu")
        },
        1446 => new List<WardDto>
        {
            new("20201", 1446, "Phường Bến Nghé"),
            new("20202", 1446, "Phường Bến Thành")
        },
        _ => new List<WardDto>
        {
            new($"{districtId}01", districtId, "Phường Trung Tâm")
        }
    };

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
