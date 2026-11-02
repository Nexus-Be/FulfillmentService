namespace Fulfillment.Api.Tests.Controllers;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Fulfillment.Api.Tests.Infrastructure;
using Fulfillment.Application.Services.Warehouse;

public class WarehousesControllerTests : IClassFixture<FulfillmentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FulfillmentWebApplicationFactory _factory;

    public WarehousesControllerTests(FulfillmentWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateWarehouse_FirstTime_Returns201Created()
    {
        var sellerId = Guid.NewGuid();
        var request = new CreateWarehouseRequest(
            Code: "WH_TEST_" + Guid.NewGuid().ToString("N")[..6],
            Name: "Ha Noi Hub Test",
            Type: "SellerWarehouse",
            Address: "123 Cau Giay, Ha Noi");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/warehouses");
        httpRequest.Headers.Add("x-user-id", sellerId.ToString());
        httpRequest.Headers.Add("x-user-roles", "seller");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WarehouseDto>();
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateWarehouse_SecondTimeForSameSeller_Returns400BadRequest()
    {
        var sellerId = Guid.NewGuid();
        var request1 = new CreateWarehouseRequest(
            Code: "WH_DUP_1_" + Guid.NewGuid().ToString("N")[..6],
            Name: "First Warehouse",
            Type: "SellerWarehouse",
            Address: "Address 1");

        using var httpRequest1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/warehouses");
        httpRequest1.Headers.Add("x-user-id", sellerId.ToString());
        httpRequest1.Headers.Add("x-user-roles", "seller");
        httpRequest1.Content = JsonContent.Create(request1);

        var response1 = await _client.SendAsync(httpRequest1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var request2 = new CreateWarehouseRequest(
            Code: "WH_DUP_2_" + Guid.NewGuid().ToString("N")[..6],
            Name: "Second Warehouse Attempt",
            Type: "SellerWarehouse",
            Address: "Address 2");

        using var httpRequest2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/warehouses");
        httpRequest2.Headers.Add("x-user-id", sellerId.ToString());
        httpRequest2.Headers.Add("x-user-roles", "seller");
        httpRequest2.Content = JsonContent.Create(request2);

        var response2 = await _client.SendAsync(httpRequest2);

        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);

        var responseContent = await response2.Content.ReadAsStringAsync();
        Assert.Contains("Each seller is allowed only one warehouse", responseContent);
    }

    [Fact]
    public async Task CreateWarehouse_MissingUserIdHeader_Returns401Unauthorized()
    {
        var request = new CreateWarehouseRequest(
            Code: "WH_UNAUTH",
            Name: "Unauthorized Warehouse",
            Type: "SellerWarehouse",
            Address: "No header address");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/warehouses");
        httpRequest.Headers.Add("x-user-roles", "seller");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateWarehouse_InvalidRole_Returns403Forbidden()
    {
        var sellerId = Guid.NewGuid();
        var request = new CreateWarehouseRequest(
            Code: "WH_BUYER_" + Guid.NewGuid().ToString("N")[..6],
            Name: "Buyer Cannot Create Warehouse",
            Type: "SellerWarehouse",
            Address: "Address");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/warehouses");
        httpRequest.Headers.Add("x-user-id", sellerId.ToString());
        httpRequest.Headers.Add("x-user-roles", "buyer");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListActive_Returns200WithPagination()
    {
        var response = await _client.GetAsync("/api/v1/warehouses?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("items", content);
        Assert.Contains("pagination", content);
    }

    [Fact]
    public async Task InternalCreate_ValidServiceKey_Returns201Created()
    {
        var sellerId = Guid.NewGuid();
        var request = new InternalCreateWarehouseRequest(
            SellerId: sellerId,
            UserId: Guid.NewGuid(),
            ShopName: "Shop Auto Created",
            ContactName: "Tran Van C",
            ContactPhone: "0901234567",
            PickupAddress: "456 Hai Ba Trung, Ha Noi",
            ProvinceId: 201,
            DistrictId: 1490,
            WardCode: "1A0807");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/internal/warehouses");
        httpRequest.Headers.Add("X-Internal-Service-Key", "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<InternalWarehouseResponse>();
        Assert.NotNull(result);
        Assert.Equal(sellerId, result.SellerId);
        Assert.Equal("ACTIVE", result.Status);
    }

    [Fact]
    public async Task InternalCreate_WithoutServiceKey_Returns201Created()
    {
        var request = new InternalCreateWarehouseRequest(
            SellerId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            ShopName: "Shop No Key",
            ContactName: null,
            ContactPhone: null,
            PickupAddress: "123 Street",
            ProvinceId: null,
            DistrictId: null,
            WardCode: null);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/internal/warehouses");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task InternalUpdate_ExistingSeller_Returns200Ok()
    {
        var sellerId = Guid.NewGuid();
        var createRequest = new InternalCreateWarehouseRequest(
            SellerId: sellerId,
            UserId: Guid.NewGuid(),
            ShopName: "Original Shop",
            ContactName: null,
            ContactPhone: null,
            PickupAddress: "Original Address",
            ProvinceId: null,
            DistrictId: null,
            WardCode: null);

        using var createMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/internal/warehouses");
        createMsg.Content = JsonContent.Create(createRequest);
        var createRes = await _client.SendAsync(createMsg);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var updateRequest = new InternalUpdateWarehouseRequest(
            ShopName: "Updated Shop Name",
            ContactPhone: "0987654321",
            PickupAddress: "789 Updated St",
            ProvinceId: 201,
            DistrictId: 1490,
            WardCode: "1A0807");

        using var updateMsg = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/internal/warehouses/{sellerId}");
        updateMsg.Content = JsonContent.Create(updateRequest);
        var updateRes = await _client.SendAsync(updateMsg);

        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
        var updated = await updateRes.Content.ReadFromJsonAsync<WarehouseDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Shop Name", updated.Name);
        Assert.Equal("789 Updated St", updated.Address);
    }

    [Fact]
    public async Task InternalDelete_ExistingSeller_Returns204NoContent()
    {
        var sellerId = Guid.NewGuid();
        var createRequest = new InternalCreateWarehouseRequest(
            SellerId: sellerId,
            UserId: Guid.NewGuid(),
            ShopName: "Shop To Delete",
            ContactName: null,
            ContactPhone: null,
            PickupAddress: "Temp Address",
            ProvinceId: null,
            DistrictId: null,
            WardCode: null);

        using var createMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/internal/warehouses");
        createMsg.Content = JsonContent.Create(createRequest);
        var createRes = await _client.SendAsync(createMsg);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        using var deleteMsg = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/internal/warehouses/{sellerId}");
        var deleteRes = await _client.SendAsync(deleteMsg);

        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);
    }
}

