namespace Fulfillment.Api.Tests.Controllers;

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Fulfillment.Api.Tests.Infrastructure;
using Fulfillment.Application.Services.Shipping;
using Xunit;

public class ShippingControllerTests : IClassFixture<FulfillmentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string InternalKey = "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e";

    public ShippingControllerTests(FulfillmentWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProvinces_Public_Returns200WithList()
    {
        var response = await _client.GetAsync("/api/v1/shipping/locations/provinces");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var provinces = await response.Content.ReadFromJsonAsync<List<ProvinceDto>>();
        Assert.NotNull(provinces);
        Assert.NotEmpty(provinces);
    }

    [Fact]
    public async Task GetDistricts_ValidProvince_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/shipping/locations/districts?provinceId=201");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var districts = await response.Content.ReadFromJsonAsync<List<DistrictDto>>();
        Assert.NotNull(districts);
        Assert.NotEmpty(districts);
    }

    [Fact]
    public async Task GetDistricts_InvalidProvinceId_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/shipping/locations/districts?provinceId=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetWards_ValidDistrict_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/shipping/locations/wards?districtId=1442");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var wards = await response.Content.ReadFromJsonAsync<List<WardDto>>();
        Assert.NotNull(wards);
        Assert.NotEmpty(wards);
    }

    [Fact]
    public async Task ValidateLocation_Internal_ValidHierarchy_Returns200()
    {
        var request = new ValidateShippingLocationRequest(
            ProvinceId: 201,
            DistrictId: 1442,
            WardCode: "20101");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/internal/shipping/locations/validate");
        httpRequest.Headers.Add("X-Internal-Service-Key", InternalKey);
        httpRequest.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ShippingLocationValidationResponse>();
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal("Hà Nội", result.ProvinceName);
    }

    [Fact]
    public async Task ValidateLocation_Internal_InvalidHierarchy_Returns400()
    {
        var request = new ValidateShippingLocationRequest(
            ProvinceId: 201, // Hà Nội
            DistrictId: 1446, // Quận 1 (belongs to TP.HCM)
            WardCode: "20101");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/internal/shipping/locations/validate");
        httpRequest.Headers.Add("X-Internal-Service-Key", InternalKey);
        httpRequest.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
