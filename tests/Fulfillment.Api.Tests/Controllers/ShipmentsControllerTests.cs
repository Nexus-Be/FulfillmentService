namespace Fulfillment.Api.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Fulfillment.Api.Tests.Infrastructure;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Entities;
using Fulfillment.Infrastructure.Persistence;

public class ShipmentsControllerTests : IClassFixture<FulfillmentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FulfillmentWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public ShipmentsControllerTests(FulfillmentWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = factory.CreateClient();
    }

    private async Task EnsureTestDataSeededAsync(Guid sellerId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Carriers.AnyAsync(c => c.Code == "GHN"))
        {
            db.Carriers.Add(new Carrier
            {
                Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                Code = "GHN",
                Name = "Giao Hang Nhanh (Sandbox)",
                IsActive = true,
                Priority = 1
            });
        }

        if (!await db.Warehouses.AnyAsync(w => w.SellerId == sellerId))
        {
            db.Warehouses.Add(new Warehouse
            {
                Id = Guid.Parse("eeeeeeee-3333-3333-3333-eeeeeeeeeeee"),
                SellerId = sellerId,
                Code = "WH_SELLER_01",
                Name = "Kho Trung Tam Seller",
                Address = "123 Le Loi, Ben Thanh, Quan 1, TP.HCM",
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ShipmentModule_FullLifecycle_EndToEnd_LogsResults()
    {
        _output.WriteLine("================================================================================");
        _output.WriteLine("🚀 BẮT ĐẦU CHẠY E2E TEST: TOÀN BỘ VÒNG ĐỜI SHIPMENT MODULE (FULFILLMENT SERVICE)");
        _output.WriteLine("================================================================================\n");

        var sellerId = Guid.Parse("cccccccc-3333-3333-3333-333333333333");
        var orderId = Guid.NewGuid();
        var subOrderId = Guid.NewGuid();

        await EnsureTestDataSeededAsync(sellerId);

        // -----------------------------------------------------------------------------
        // BƯỚC 1: BÁO GIÁ CƯỚC VẬN CHUYỂN (POST /api/v1/internal/shipping/quote)
        // -----------------------------------------------------------------------------
        _output.WriteLine("👉 [BƯỚC 1] BÁO GIÁ CƯỚC VẬN CHUYỂN (POST /api/v1/internal/shipping/quote)");
        var quoteRequest = new ShopShippingQuoteRequest(
            SellerId: sellerId,
            FromDistrictId: 1442,
            ToDistrictId: 1450,
            ToWardCode: "20804",
            WeightInGrams: 500,
            Items: new List<ShippingQuoteItem>
            {
                new(Guid.NewGuid(), 1, 500)
            });

        using var quoteHttp = new HttpRequestMessage(HttpMethod.Post, "/api/v1/internal/shipping/quote");
        quoteHttp.Headers.Add("Referer", "http://localhost:8006/swagger/index.html");
        quoteHttp.Content = JsonContent.Create(quoteRequest);

        var quoteResponse = await _client.SendAsync(quoteHttp);
        var quoteRaw = await quoteResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"   Status code: {(int)quoteResponse.StatusCode} {quoteResponse.StatusCode}");
        _output.WriteLine($"   Response Body:\n{quoteRaw}");

        Assert.Equal(HttpStatusCode.OK, quoteResponse.StatusCode);
        var quoteResult = JsonSerializer.Deserialize<ShippingQuoteResponse>(quoteRaw, JsonOptions);
        Assert.NotNull(quoteResult);
        Assert.True(quoteResult.Fee > 0, "Cước vận chuyển phải > 0");
        _output.WriteLine($"   ✅ [PASS] Tính cước thành công: {quoteResult.Fee:N0} VNĐ (Dịch vụ: {quoteResult.ServiceName})\n");

        // -----------------------------------------------------------------------------
        // BƯỚC 2: TẠO VẬN ĐƠN GIAO VẬN (POST /api/v1/internal/shipments)
        // -----------------------------------------------------------------------------
        _output.WriteLine("👉 [BƯỚC 2] TẠO VẬN ĐƠN GIAO VẬN (POST /api/v1/internal/shipments)");
        var createRequest = new CreateShipmentRequest(
            OrderId: orderId,
            SubOrderId: subOrderId,
            SellerId: sellerId,
            RecipientName: "Nguyen Van A",
            RecipientPhone: "0901234567",
            ShippingAddress: "123 Le Loi, Ben Thanh, Quan 1, TP.HCM",
            ToDistrictId: 1442,
            ToWardCode: "20101",
            WeightInGrams: 500);

        using var createHttp = new HttpRequestMessage(HttpMethod.Post, "/api/v1/internal/shipments");
        createHttp.Headers.Add("Referer", "http://localhost:8006/swagger/index.html");
        createHttp.Content = JsonContent.Create(createRequest);

        var createResponse = await _client.SendAsync(createHttp);
        var createRaw = await createResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"   Status code: {(int)createResponse.StatusCode} {createResponse.StatusCode}");
        _output.WriteLine($"   Response Body:\n{createRaw}");

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var createResult = JsonSerializer.Deserialize<CreateShipmentResponse>(createRaw, JsonOptions);
        Assert.NotNull(createResult);
        Assert.False(string.IsNullOrWhiteSpace(createResult.TrackingNumber), "Tracking number không được rỗng");
        Assert.Equal("ReadyToPick", createResult.Status);

        var trackingNumber = createResult.TrackingNumber;
        var shipmentId = createResult.ShipmentId;
        _output.WriteLine($"   ✅ [PASS] Tạo vận đơn thành công: Mã={trackingNumber}, ShipmentId={shipmentId}, Status={createResult.Status}\n");

        // -----------------------------------------------------------------------------
        // BƯỚC 3: KIỂM TRA CHỐNG TRÙNG VẬN ĐƠN (IDEMPOTENCY)
        // -----------------------------------------------------------------------------
        _output.WriteLine("👉 [BƯỚC 3] KIỂM TRA TÍNH BẤT BIẾN / CHỐNG TRÙNG (IDEMPOTENCY DUPLICATE REQUEST)");
        using var duplicateHttp = new HttpRequestMessage(HttpMethod.Post, "/api/v1/internal/shipments");
        duplicateHttp.Headers.Add("Referer", "http://localhost:8006/swagger/index.html");
        duplicateHttp.Content = JsonContent.Create(createRequest);

        var duplicateResponse = await _client.SendAsync(duplicateHttp);
        var duplicateRaw = await duplicateResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"   Status code: {(int)duplicateResponse.StatusCode} {duplicateResponse.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, duplicateResponse.StatusCode);
        var duplicateResult = JsonSerializer.Deserialize<CreateShipmentResponse>(duplicateRaw, JsonOptions);
        Assert.NotNull(duplicateResult);
        Assert.Equal(shipmentId, duplicateResult.ShipmentId);
        Assert.Equal(trackingNumber, duplicateResult.TrackingNumber);
        _output.WriteLine($"   ✅ [PASS] Idempotency xác nhận: Trả về cùng ShipmentId={duplicateResult.ShipmentId}, không tạo bản ghi rác.\n");

        // -----------------------------------------------------------------------------
        // BƯỚC 4: TRA CỨU VẬN ĐƠN THEO SUB-ORDER ID (GET /api/v1/shipments/order/{subOrderId})
        // -----------------------------------------------------------------------------
        _output.WriteLine($"👉 [BƯỚC 4] TRA CỨU VẬN ĐƠN THEO SUB-ORDER ID (GET /api/v1/shipments/order/{subOrderId})");
        var getByOrderResponse = await _client.GetAsync($"/api/v1/shipments/order/{subOrderId}");
        var getByOrderRaw = await getByOrderResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"   Status code: {(int)getByOrderResponse.StatusCode} {getByOrderResponse.StatusCode}");
        _output.WriteLine($"   Response Body:\n{getByOrderRaw}");

        Assert.Equal(HttpStatusCode.OK, getByOrderResponse.StatusCode);
        var getByOrderResult = JsonSerializer.Deserialize<ShipmentDto>(getByOrderRaw, JsonOptions);
        Assert.NotNull(getByOrderResult);
        Assert.Equal(trackingNumber, getByOrderResult.TrackingNumber);
        Assert.Equal(createRequest.RecipientName, getByOrderResult.ReceiverName);
        _output.WriteLine($"   ✅ [PASS] Tra cứu theo Order thành công: Người nhận={getByOrderResult.ReceiverName}, Trạng thái={getByOrderResult.Status}\n");

        // -----------------------------------------------------------------------------
        // BƯỚC 5: TRA CỨU VẬN ĐƠN THEO TRACKING NUMBER (GET /api/v1/shipments/tracking/{trackingNumber})
        // -----------------------------------------------------------------------------
        _output.WriteLine($"👉 [BƯỚC 5] TRA CỨU VẬN ĐƠN THEO TRACKING CODE (GET /api/v1/shipments/tracking/{trackingNumber})");
        var getByTrackingResponse = await _client.GetAsync($"/api/v1/shipments/tracking/{trackingNumber}");
        var getByTrackingRaw = await getByTrackingResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"   Status code: {(int)getByTrackingResponse.StatusCode} {getByTrackingResponse.StatusCode}");
        _output.WriteLine($"   Response Body:\n{getByTrackingRaw}");

        Assert.Equal(HttpStatusCode.OK, getByTrackingResponse.StatusCode);
        var getByTrackingResult = JsonSerializer.Deserialize<ShipmentDto>(getByTrackingRaw, JsonOptions);
        Assert.NotNull(getByTrackingResult);
        Assert.Equal(shipmentId, getByTrackingResult.Id);
        Assert.Equal("ReadyToPick", getByTrackingResult.Status);
        _output.WriteLine($"   ✅ [PASS] Tra cứu theo Tracking thành công: ShipmentId={getByTrackingResult.Id}\n");

        // -----------------------------------------------------------------------------
        // BƯỚC 6: KIỂM TRA TRA CỨU MÃ KHÔNG TỒN TẠI (NEGATIVE CASE 404 NOT FOUND)
        // -----------------------------------------------------------------------------
        _output.WriteLine("👉 [BƯỚC 6] KIỂM TRA MÃ VẬN ĐƠN KHÔNG TỒN TẠI (GET /api/v1/shipments/tracking/UNKNOWN_XYZ)");
        var notFoundResponse = await _client.GetAsync("/api/v1/shipments/tracking/UNKNOWN_TRACKING_12345");
        var notFoundRaw = await notFoundResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"   Status code: {(int)notFoundResponse.StatusCode} {notFoundResponse.StatusCode}");
        _output.WriteLine($"   Response Body (ProblemDetails RFC 9110):\n{notFoundRaw}");

        Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
        Assert.Contains("SHIPMENT_NOT_FOUND", notFoundRaw);
        _output.WriteLine("   ✅ [PASS] Bắt lỗi 404 Not Found đúng chuẩn RFC 9110 ProblemDetails.\n");

        // -----------------------------------------------------------------------------
        // BƯỚC 7: XỬ LÝ GHN WEBHOOK CẬP NHẬT ĐANG GIAO HÀNG (POST /api/v1/webhooks/ghn)
        // -----------------------------------------------------------------------------
        _output.WriteLine("👉 [BƯỚC 7] XỬ LÝ GHN WEBHOOK: BÁO ĐANG GIAO HÀNG (POST /api/v1/webhooks/ghn)");
        var webhookDelivering = new
        {
            OrderCode = trackingNumber,
            Status = "delivering",
            Fee = 32000,
            Time = 1730700000
        };

        var webhookResponse1 = await _client.PostAsJsonAsync("/api/v1/webhooks/ghn", webhookDelivering);
        var webhookRaw1 = await webhookResponse1.Content.ReadAsStringAsync();
        _output.WriteLine($"   Status code: {(int)webhookResponse1.StatusCode} {webhookResponse1.StatusCode}");
        _output.WriteLine($"   Response Body:\n{webhookRaw1}");

        Assert.Equal(HttpStatusCode.OK, webhookResponse1.StatusCode);
        _output.WriteLine("   ✅ [PASS] Webhook GHN 'delivering' xử lý thành công.\n");

        // -----------------------------------------------------------------------------
        // BƯỚC 8: XÁC MINH TRẠNG THÁI DB ĐÃ CHUYỂN SANG 'InTransit'
        // -----------------------------------------------------------------------------
        _output.WriteLine($"👉 [BƯỚC 8] XÁC MINH TRẠNG THÁI TRONG DB SAU KHI WEBHOOK GỌI (GET tracking/{trackingNumber})");
        var inTransitResponse = await _client.GetAsync($"/api/v1/shipments/tracking/{trackingNumber}");
        var inTransitRaw = await inTransitResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"   Response Body:\n{inTransitRaw}");

        var inTransitResult = JsonSerializer.Deserialize<ShipmentDto>(inTransitRaw, JsonOptions);
        Assert.NotNull(inTransitResult);
        Assert.Equal("InTransit", inTransitResult.Status);
        _output.WriteLine($"   ✅ [PASS] Trạng thái vận đơn đã chuyển sang '{inTransitResult.Status}'.\n");

        // -----------------------------------------------------------------------------
        // BƯỚC 9: XỬ LÝ GHN WEBHOOK BÁO ĐÃ GIAO THÀNH CÔNG (POST /api/v1/webhooks/ghn)
        // -----------------------------------------------------------------------------
        _output.WriteLine("👉 [BƯỚC 9] XỬ LÝ GHN WEBHOOK: BÁO ĐÃ GIAO THÀNH CÔNG (POST /api/v1/webhooks/ghn)");
        var webhookDelivered = new
        {
            OrderCode = trackingNumber,
            Status = "delivered",
            Fee = 32000,
            Time = 1730703600
        };

        var webhookResponse2 = await _client.PostAsJsonAsync("/api/v1/webhooks/ghn", webhookDelivered);
        var webhookRaw2 = await webhookResponse2.Content.ReadAsStringAsync();
        _output.WriteLine($"   Status code: {(int)webhookResponse2.StatusCode} {webhookResponse2.StatusCode}");
        _output.WriteLine($"   Response Body:\n{webhookRaw2}");

        Assert.Equal(HttpStatusCode.OK, webhookResponse2.StatusCode);
        _output.WriteLine("   ✅ [PASS] Webhook GHN 'delivered' xử lý thành công.\n");

        // -----------------------------------------------------------------------------
        // BƯỚC 10: XÁC MINH TRẠNG THÁI CUỐI CÙNG LÀ 'Delivered'
        // -----------------------------------------------------------------------------
        _output.WriteLine($"👉 [BƯỚC 10] XÁC MINH TRẠNG THÁI HOÀN TẤT TRONG DB (GET tracking/{trackingNumber})");
        var deliveredResponse = await _client.GetAsync($"/api/v1/shipments/tracking/{trackingNumber}");
        var deliveredRaw = await deliveredResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"   Response Body:\n{deliveredRaw}");

        var deliveredResult = JsonSerializer.Deserialize<ShipmentDto>(deliveredRaw, JsonOptions);
        Assert.NotNull(deliveredResult);
        Assert.Equal("Delivered", deliveredResult.Status);
        _output.WriteLine($"   ✅ [PASS] Trạng thái vận đơn cuối cùng là '{deliveredResult.Status}'.\n");

        _output.WriteLine("================================================================================");
        _output.WriteLine("🎉 HOÀN THÀNH TOÀN BỘ 10 BƯỚC E2E TEST: SHIPMENT MODULE HOẠT ĐỘNG HOÀN HẢO! 100% PASS");
        _output.WriteLine("================================================================================");
    }
}
