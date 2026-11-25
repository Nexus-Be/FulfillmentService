namespace Fulfillment.Api.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Api.Tests.Infrastructure;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ShippingControllerTests : IClassFixture<FulfillmentWebApplicationFactory>
{
    private const string InternalKey = "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e";
    private static readonly DateTimeOffset Observed = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Answers the way the carrier does: real province, district and ward ids, and the origin of every list.</summary>
    private sealed class CarrierLists(bool down) : IShippingLocationService
    {
        private static LocationList<T> List<T>(IReadOnlyList<T> items) => new(items, LocationSources.GhnCache, Observed);

        private LocationList<T> Answer<T>(IReadOnlyList<T> items) =>
            down ? throw new BusinessRuleValidationException("LOCATION_SOURCE_UNAVAILABLE", "Shipping locations are temporarily unavailable.", 503) : List(items);

        public Task<LocationList<ProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Answer<ProvinceDto>(new[] { new ProvinceDto(201, "Ha Noi", "01"), new ProvinceDto(202, "Ho Chi Minh", "79") }));

        public Task<LocationList<DistrictDto>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Answer<DistrictDto>(provinceId == 202
                ? new[] { new DistrictDto(1442, 202, "Quan 1", 3), new DistrictDto(1446, 202, "Quan 4", 3) }
                : new[] { new DistrictDto(1485, 201, "Cau Giay", 3) }));

        public Task<LocationList<WardDto>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Answer<WardDto>(districtId == 1442 ? new[] { new WardDto("20101", 1442, "Phuong Ben Nghe") } : Array.Empty<WardDto>()));

        public Task<IReadOnlyList<AvailableShippingServiceDto>> GetAvailableServicesAsync(int fromDistrictId, int toDistrictId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AvailableShippingServiceDto>>(new[] { new AvailableShippingServiceDto(53320, "Standard", 2) });

        public async Task<ShippingLocationValidationResponse> ValidateLocationAsync(ValidateShippingLocationRequest request, CancellationToken cancellationToken = default)
        {
            var provinces = (await GetProvincesAsync(cancellationToken)).Items;
            var province = provinces.FirstOrDefault(p => p.ProvinceId == request.ProvinceId);
            var districts = (await GetDistrictsAsync(request.ProvinceId, cancellationToken)).Items;
            var district = districts.FirstOrDefault(d => d.DistrictId == request.DistrictId);
            var wards = (await GetWardsAsync(request.DistrictId, cancellationToken)).Items;
            var ward = wards.FirstOrDefault(w => w.WardCode == request.WardCode);
            return province is null || district is null || ward is null
                ? new ShippingLocationValidationResponse(false, request.ProvinceId, province?.ProvinceName, request.DistrictId, district?.DistrictName, request.WardCode, ward?.WardName, "The location hierarchy is not valid.")
                : new ShippingLocationValidationResponse(true, request.ProvinceId, province.ProvinceName, request.DistrictId, district.DistrictName, ward.WardCode, ward.WardName);
        }
    }

    private readonly FulfillmentWebApplicationFactory _factory;

    public ShippingControllerTests(FulfillmentWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client(bool down = false) => _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
    {
        foreach (var d in services.Where(x => x.ServiceType == typeof(IShippingLocationService)).ToList()) services.Remove(d);
        services.AddSingleton<IShippingLocationService>(new CarrierLists(down));
    })).CreateClient();

    private static HttpRequestMessage Validate(int province, int district, string ward)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/internal/shipping/locations/validate");
        request.Headers.Add("X-Internal-Service-Key", InternalKey);
        request.Content = JsonContent.Create(new ValidateShippingLocationRequest(province, district, ward));
        return request;
    }

    [Fact]
    public async Task GetProvinces_Public_Returns200WithPlainArrayAndSourceHeaders()
    {
        var response = await Client().GetAsync("/api/v1/shipping/locations/provinces");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var provinces = await response.Content.ReadFromJsonAsync<List<ProvinceDto>>();
        Assert.NotNull(provinces);
        Assert.Equal(2, provinces.Count);
        Assert.Equal("ghn-cache", response.Headers.GetValues("X-Location-Source").Single());
        Assert.StartsWith("2026-11-24T10:00:00", response.Headers.GetValues("X-Location-Observed-At").Single());
    }

    [Fact]
    public async Task GetDistricts_ValidProvince_Returns200()
    {
        var response = await Client().GetAsync("/api/v1/shipping/locations/districts?provinceId=202");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var districts = await response.Content.ReadFromJsonAsync<List<DistrictDto>>();
        Assert.NotNull(districts);
        Assert.Contains(districts, d => d.DistrictId == 1442 && d.DistrictName == "Quan 1");
    }

    [Fact]
    public async Task GetDistricts_InvalidProvinceId_Returns400()
    {
        var response = await Client().GetAsync("/api/v1/shipping/locations/districts?provinceId=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetWards_ValidDistrict_Returns200()
    {
        var response = await Client().GetAsync("/api/v1/shipping/locations/wards?districtId=1442");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var wards = await response.Content.ReadFromJsonAsync<List<WardDto>>();
        Assert.NotNull(wards);
        Assert.Contains(wards, w => w.WardCode == "20101");
    }

    [Theory]
    [InlineData("/api/v1/shipping/locations/provinces")]
    [InlineData("/api/v1/shipping/locations/districts?provinceId=202")]
    [InlineData("/api/v1/shipping/locations/wards?districtId=1442")]
    public async Task A_list_with_no_trusted_source_is_503_with_its_own_code(string path)
    {
        var response = await Client(down: true).GetAsync(path);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("LOCATION_SOURCE_UNAVAILABLE", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ValidateLocation_Internal_ValidHierarchy_Returns200()
    {
        var response = await Client().SendAsync(Validate(202, 1442, "20101"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ShippingLocationValidationResponse>();
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal("Ho Chi Minh", result.ProvinceName);
    }

    [Fact]
    public async Task ValidateLocation_Internal_InvalidHierarchy_Returns400()
    {
        var response = await Client().SendAsync(Validate(201, 1446, "20101"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("VAL_INVALID_LOCATION", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ValidateLocation_Internal_WithNoTrustedSource_Is503_NotAnInvalidAddress()
    {
        var response = await Client(down: true).SendAsync(Validate(202, 1442, "20101"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("LOCATION_SOURCE_UNAVAILABLE", body);
        Assert.DoesNotContain("VAL_INVALID_LOCATION", body);
    }
}
