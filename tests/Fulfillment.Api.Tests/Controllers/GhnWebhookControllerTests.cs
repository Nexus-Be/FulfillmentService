namespace Fulfillment.Api.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using Fulfillment.Api.Tests.Infrastructure;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// The GHN webhook can mark a shipment delivered (which lets the buyer release the seller's payout) or cancel an order,
/// so it must be closed to anyone without the shared secret, must never move a shipment backwards, and must not lose a
/// delivery update when Commerce is down.
/// </summary>
public sealed class GhnWebhookControllerTests : IClassFixture<FulfillmentWebApplicationFactory>, IClassFixture<UnconfiguredWebhookFactory>
{
    private static readonly string[] Routes =
    {
        "/webhooks/ghn", "/api/v1/webhooks/ghn", "/api/v1/webhooks/carriers/ghn", "/api/v1/fulfillment/webhook/ghn", "/api/v1/fulfillment/webhooks/ghn"
    };

    private readonly FulfillmentWebApplicationFactory _factory;
    private readonly UnconfiguredWebhookFactory _unconfigured;

    public GhnWebhookControllerTests(FulfillmentWebApplicationFactory factory, UnconfiguredWebhookFactory unconfigured)
    {
        _factory = factory;
        _unconfigured = unconfigured;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task EveryRoute_RefusesACallWithoutTheSecret(int route)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(Routes[route], Payload("GHN-X", "delivered"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongSecret_IsRefused_AndChangesNothing()
    {
        var orderId = await SeedShipmentAsync(_factory, "GHN-WRONG", ShipmentStatus.InTransit);

        var response = await Post(_factory, "/api/v1/webhooks/ghn", Payload("GHN-WRONG", "delivered"), header: "not-the-secret");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ShipmentStatus.InTransit, await StatusOf(_factory, "GHN-WRONG"));
        Assert.DoesNotContain(_factory.Commerce.Sent, s => s.OrderId == orderId);
    }

    [Fact]
    public async Task SecretInTheHeader_IsAccepted()
    {
        var response = await Post(_factory, "/api/v1/webhooks/ghn", Payload("GHN-UNKNOWN-1", "picked"), header: FulfillmentWebApplicationFactory.WebhookSecret);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SecretInTheQueryString_IsAccepted_BecauseGhnOnlyLetsUsConfigureAUrl()
    {
        var response = await Post(_factory, $"/api/v1/webhooks/ghn?token={FulfillmentWebApplicationFactory.WebhookSecret}", Payload("GHN-UNKNOWN-2", "picked"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WithoutAConfiguredSecret_TheWebhookStaysClosed_EvenForAnEmptyToken()
    {
        var withToken = await Post(_unconfigured, "/api/v1/webhooks/ghn", Payload("GHN-X", "delivered"), header: "anything");
        var withoutToken = await Post(_unconfigured, "/api/v1/webhooks/ghn", Payload("GHN-X", "delivered"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, withToken.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, withoutToken.StatusCode);
    }

    [Fact]
    public async Task LateEvent_NeverMovesAShipmentBackwards_AndCommerceIsNotToldAgain()
    {
        var orderId = await SeedShipmentAsync(_factory, "GHN-LATE", ShipmentStatus.ReadyToPick);

        Assert.Equal(HttpStatusCode.OK, (await Authorised(_factory, Payload("GHN-LATE", "delivering"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Authorised(_factory, Payload("GHN-LATE", "delivered"))).StatusCode);
        var late = await Authorised(_factory, Payload("GHN-LATE", "delivering", description: "late"));

        Assert.Equal(HttpStatusCode.OK, late.StatusCode);     // acknowledged so the carrier stops retrying
        Assert.Equal(ShipmentStatus.Delivered, await StatusOf(_factory, "GHN-LATE"));
        Assert.Equal(new[] { "InTransit", "Delivered" }, _factory.Commerce.Sent.Where(s => s.OrderId == orderId).Select(s => s.Status));
    }

    [Fact]
    public async Task UnknownCarrierStatus_CannotResetAShipment()
    {
        await SeedShipmentAsync(_factory, "GHN-ODD", ShipmentStatus.InTransit);

        await Authorised(_factory, Payload("GHN-ODD", "some_new_ghn_status"));

        Assert.Equal(ShipmentStatus.InTransit, await StatusOf(_factory, "GHN-ODD"));
    }

    [Fact]
    public async Task WhenCommerceIsDown_TheUpdateStaysQueued_AndTheWebhookStillSucceeds()
    {
        var factory = new FulfillmentWebApplicationFactory();
        factory.Commerce.Acknowledges = false;
        var orderId = await SeedShipmentAsync(factory, "GHN-DOWN", ShipmentStatus.InTransit);

        var response = await Authorised(factory, Payload("GHN-DOWN", "delivered"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ShipmentStatus.Delivered, await StatusOf(factory, "GHN-DOWN"));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = await db.WebhookAuditLogs.SingleAsync(l => l.OrderCode == "GHN-DOWN");
        Assert.Equal(orderId, log.NotifyOrderId);
        Assert.Equal("Delivered", log.NotifyStatus);
        Assert.Null(log.CommerceNotifiedAt);                   // not acknowledged, so the worker will retry it
        Assert.Equal(1, log.CommerceNotifyAttempts);
        Assert.NotNull(log.CommerceNotifyNextRetryAt);
    }

    [Fact]
    public async Task WhenCommerceAcknowledges_TheUpdateIsMarkedDone()
    {
        var orderId = await SeedShipmentAsync(_factory, "GHN-OK", ShipmentStatus.InTransit);

        await Authorised(_factory, Payload("GHN-OK", "delivered"));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = await db.WebhookAuditLogs.SingleAsync(l => l.OrderCode == "GHN-OK");
        Assert.NotNull(log.CommerceNotifiedAt);
        Assert.Contains(_factory.Commerce.Sent, s => s.OrderId == orderId && s.Status == "Delivered");
    }

    private static object Payload(string orderCode, string status, string? description = null) =>
        new { OrderCode = orderCode, Status = status, Description = description };

    private static Task<HttpResponseMessage> Authorised(FulfillmentWebApplicationFactory factory, object payload) =>
        Post(factory, "/api/v1/webhooks/ghn", payload, header: FulfillmentWebApplicationFactory.WebhookSecret);

    private static Task<HttpResponseMessage> Post(FulfillmentWebApplicationFactory factory, string url, object payload, string? header = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(payload) };
        if (header is not null)
            request.Headers.Add("X-Webhook-Token", header);
        return factory.CreateClient().SendAsync(request);
    }

    private static async Task<Guid> SeedShipmentAsync(FulfillmentWebApplicationFactory factory, string tracking, ShipmentStatus status)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orderId = Guid.NewGuid();
        db.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(), OrderId = orderId, WarehouseId = Guid.NewGuid(), CarrierId = Guid.NewGuid(),
            TrackingNumber = tracking, GhnOrderCode = tracking, Status = status,
            ReceiverName = "Buyer", ReceiverPhone = "0912345678", ReceiverAddress = "456 Tran Hung Dao"
        });
        await db.SaveChangesAsync();
        return orderId;
    }

    private static async Task<ShipmentStatus> StatusOf(FulfillmentWebApplicationFactory factory, string tracking)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.Shipments.AsNoTracking().SingleAsync(s => s.TrackingNumber == tracking)).Status;
    }
}
