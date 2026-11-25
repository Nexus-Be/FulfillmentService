namespace Fulfillment.Api.Tests.Controllers;

using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Fulfillment.Api.Tests.Infrastructure;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class GhnWebhookE2ETests : IClassFixture<FulfillmentWebApplicationFactory>
{
    private readonly FulfillmentWebApplicationFactory _factory;

    public GhnWebhookE2ETests(FulfillmentWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Webhook_WithValidSecret_UpdatesShipment_AndEnqueuesOutbox()
    {
        var orderId = await SeedShipmentAsync(_factory, "GHN-E2E-1", ShipmentStatus.ReadyToPick);

        var payload = new
        {
            OrderCode = "GHN-E2E-1",
            Status = "delivering",
            Description = "Shipper đang giao hàng",
            ShipperName = "Nguyễn Văn Shipper",
            ShipperPhone = "0901234567"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/ghn")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-Webhook-Secret", FulfillmentWebApplicationFactory.WebhookSecret);

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var shipment = await db.Shipments.Include(s => s.Events).SingleAsync(s => s.TrackingNumber == "GHN-E2E-1");
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);

        var auditLog = await db.WebhookAuditLogs.SingleAsync(l => l.OrderCode == "GHN-E2E-1");
        Assert.True(auditLog.IsProcessed);
        Assert.Equal("InTransit", auditLog.NotifyStatus);
        Assert.Equal(orderId, auditLog.NotifyOrderId);

        Assert.Contains(shipment.Events, e => e.ShipperName == "Nguyễn Văn Shipper" && e.ShipperPhone == "0901234567");
    }

    [Fact]
    public async Task Webhook_WithThreeConsecutiveFailedDeliveries_AutoTransitionsToReturned()
    {
        var orderId = await SeedShipmentAsync(_factory, "GHN-3FAIL", ShipmentStatus.InTransit);

        for (int i = 1; i <= 3; i++)
        {
            var payload = new
            {
                OrderCode = "GHN-3FAIL",
                Status = "delivery_fail",
                Description = $"Giao thất bại lần {i}",
                Reason = "Khách không nghe máy",
                Time = $"2026-11-25T14:{i:D2}:00Z"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/ghn")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("X-Webhook-Token", FulfillmentWebApplicationFactory.WebhookSecret);

            var response = await _factory.CreateClient().SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var shipment = await db.Shipments.SingleAsync(s => s.TrackingNumber == "GHN-3FAIL");
        Assert.Equal(ShipmentStatus.Returned, shipment.Status);
    }

    private static async Task<Guid> SeedShipmentAsync(FulfillmentWebApplicationFactory factory, string tracking, ShipmentStatus status)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orderId = Guid.NewGuid();
        db.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            WarehouseId = Guid.NewGuid(),
            CarrierId = Guid.NewGuid(),
            TrackingNumber = tracking,
            GhnOrderCode = tracking,
            Status = status,
            ReceiverName = "Test Buyer",
            ReceiverPhone = "0987654321",
            ReceiverAddress = "72 Le Thanh Ton"
        });
        await db.SaveChangesAsync();
        return orderId;
    }
}
