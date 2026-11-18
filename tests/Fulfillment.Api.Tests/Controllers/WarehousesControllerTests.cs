namespace Fulfillment.Api.Tests.Controllers;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Fulfillment.Api.Tests.Infrastructure;
using Fulfillment.Application.Services.Inventory;
using Fulfillment.Application.Services.Warehouse;
using Fulfillment.Domain.Enums;

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
    public async Task FirstIntake_CreatesBalanceOnlyInOwnedWarehouse()
    {
        var sellerId = Guid.NewGuid();
        var otherSellerId = Guid.NewGuid();
        var warehouseRequest = new CreateWarehouseRequest(
            Code: "WH_INTAKE_" + Guid.NewGuid().ToString("N")[..6],
            Name: "Seller intake",
            Address: "Address",
            Type: "SellerWarehouse");
        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/v1/warehouses");
        create.Headers.Add("x-user-id", sellerId.ToString());
        create.Headers.Add("x-user-roles", "seller");
        create.Content = JsonContent.Create(warehouseRequest);
        var created = await _client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var warehouse = await created.Content.ReadFromJsonAsync<WarehouseDto>();
        Assert.NotNull(warehouse);

        var skuId = Guid.NewGuid();
        var intake = new AdjustInventoryRequest(skuId, InventoryAdjustmentType.Intake, 7, "First intake", warehouse.Id);
        using var forbiddenRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/inventories/adjust");
        forbiddenRequest.Headers.Add("x-user-id", otherSellerId.ToString());
        forbiddenRequest.Headers.Add("x-user-roles", "seller");
        forbiddenRequest.Content = JsonContent.Create(intake);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(forbiddenRequest)).StatusCode);

        using var ownedRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/inventories/adjust");
        ownedRequest.Headers.Add("x-user-id", sellerId.ToString());
        ownedRequest.Headers.Add("x-user-roles", "seller");
        ownedRequest.Content = JsonContent.Create(intake);
        var response = await _client.SendAsync(ownedRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var balance = await response.Content.ReadFromJsonAsync<AdjustInventoryResponse>();
        Assert.Equal(7, balance?.TotalQuantity);
    }

    [Fact]
    public async Task StaffCanInitializeZeroInventoryForSellerWarehouse()
    {
        var sellerId = Guid.NewGuid();
        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/v1/warehouses");
        create.Headers.Add("x-user-id", sellerId.ToString());
        create.Headers.Add("x-user-roles", "seller");
        create.Content = JsonContent.Create(new CreateWarehouseRequest(
            Code: "WH_STAFF_" + Guid.NewGuid().ToString("N")[..6],
            Name: "Seller warehouse",
            Address: "Address",
            Type: "SellerWarehouse"));
        var created = await _client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var warehouse = await created.Content.ReadFromJsonAsync<WarehouseDto>();
        Assert.NotNull(warehouse);

        using var initialize = new HttpRequestMessage(HttpMethod.Post, "/api/v1/inventories/initialize");
        initialize.Headers.Add("x-user-id", Guid.NewGuid().ToString());
        initialize.Headers.Add("x-user-roles", "staff");
        initialize.Content = JsonContent.Create(new DeclareInventoryRequest(Guid.NewGuid(), warehouse.Id, 0));
        var response = await _client.SendAsync(initialize);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, (await response.Content.ReadFromJsonAsync<InventoryItemResponse>())?.TotalQuantity);

        using var positive = new HttpRequestMessage(HttpMethod.Post, "/api/v1/inventories/initialize");
        positive.Headers.Add("x-user-id", Guid.NewGuid().ToString());
        positive.Headers.Add("x-user-roles", "staff");
        positive.Content = JsonContent.Create(new DeclareInventoryRequest(Guid.NewGuid(), warehouse.Id, 5));
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(positive)).StatusCode);
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
        Assert.Contains("SELLER_ALREADY_HAS_WAREHOUSE", responseContent);
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
    public async Task InternalCreate_MissingServiceKey_Returns403Forbidden()
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

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PublicInitialize_WithoutAdminRole_ReturnsUnauthorizedOrForbidden()
    {
        var request = new InternalCreateWarehouseRequest(
            SellerId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            ShopName: "Shop Public Exploit",
            ContactName: null,
            ContactPhone: null,
            PickupAddress: "123 Street",
            ProvinceId: null,
            DistrictId: null,
            WardCode: null);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/warehouses/initialize");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(httpRequest);

        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InternalWarehouse_BySellerId_OperationsWorkWithServiceKey()
    {
        var sellerId = Guid.NewGuid();
        var createRequest = new InternalCreateWarehouseRequest(
            SellerId: sellerId,
            UserId: sellerId,
            ShopName: "Internal Seller WH",
            ContactName: "Owner",
            ContactPhone: "0900000000",
            PickupAddress: "123 St",
            ProvinceId: 201,
            DistrictId: 1442,
            WardCode: "20101");

        // 1. Create
        using var createMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/internal/warehouses");
        createMsg.Headers.Add("X-Internal-Service-Key", "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e");
        createMsg.Content = JsonContent.Create(createRequest);
        var createRes = await _client.SendAsync(createMsg);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        // 2. Get by sellerId
        using var getMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/internal/warehouses/seller/{sellerId}");
        getMsg.Headers.Add("X-Internal-Service-Key", "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e");
        var getRes = await _client.SendAsync(getMsg);
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);

        // 3. Update by sellerId
        var updateRequest = new InternalUpdateSellerWarehouseRequest(
            ShopName: "Updated Shop",
            PickupAddress: "456 Updated St",
            ContactPhone: "0911111111",
            ProvinceId: 201,
            DistrictId: 1442,
            WardCode: "20101");
        using var updateMsg = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/internal/warehouses/seller/{sellerId}");
        updateMsg.Headers.Add("X-Internal-Service-Key", "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e");
        updateMsg.Content = JsonContent.Create(updateRequest);
        var updateRes = await _client.SendAsync(updateMsg);
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

        // 4. Delete by sellerId
        using var deleteMsg = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/internal/warehouses/seller/{sellerId}");
        deleteMsg.Headers.Add("X-Internal-Service-Key", "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e");
        var deleteRes = await _client.SendAsync(deleteMsg);
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);
    }
}

