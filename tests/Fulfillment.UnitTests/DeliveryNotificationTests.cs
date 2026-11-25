namespace Fulfillment.UnitTests;

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Infrastructure.HttpClients;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class ShipmentStatusTransitionsTests
{
    [Theory]
    [InlineData(ShipmentStatus.ReadyToPick, ShipmentStatus.Picking, true)]
    [InlineData(ShipmentStatus.ReadyToPick, ShipmentStatus.InTransit, true)]
    [InlineData(ShipmentStatus.HandedOver, ShipmentStatus.InTransit, true)]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Delivered, true)]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.Returned, true)]
    [InlineData(ShipmentStatus.DeliveryFailed, ShipmentStatus.InTransit, true)]
    [InlineData(ShipmentStatus.DeliveryFailed, ShipmentStatus.Delivered, true)]
    // never backwards, and a repeat is not progress
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.InTransit, false)]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.Delivered, false)]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.DeliveryFailed, false)]   // a late carrier "delivery failed" after delivery
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.Cancelled, false)]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Picking, false)]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.ReadyToPick, false)]   // what an unknown carrier status maps to
    [InlineData(ShipmentStatus.DeliveryFailed, ShipmentStatus.Picking, false)]
    // the end of the road
    [InlineData(ShipmentStatus.Returned, ShipmentStatus.Delivered, false)]
    [InlineData(ShipmentStatus.Cancelled, ShipmentStatus.InTransit, false)]
    public void IsForward_OnlyAcceptsRealProgress(ShipmentStatus current, ShipmentStatus next, bool expected)
    {
        ShipmentStatusTransitions.IsForward(current, next).Should().Be(expected);
    }
}

public sealed class DeliveryNotificationOutboxTests
{
    [Fact]
    public async Task TrySend_WhenCommerceAcknowledges_MarksItNotified()
    {
        using var db = CreateDb();
        var log = await AddPendingAsync(db);
        var commerce = new FakeCommerce();

        var delivered = await CreateOutbox(db, commerce).TrySendAsync(log);

        delivered.Should().BeTrue();
        log.CommerceNotifiedAt.Should().NotBeNull();
        log.CommerceNotifyNextRetryAt.Should().BeNull();
        var sent = commerce.Sent.Should().ContainSingle().Subject;
        sent.Status.Should().Be("Delivered");
        sent.OccurredAt.Should().Be(log.ReceivedAt);       // the carrier's time, not the time of the retry
    }

    [Fact]
    public async Task TrySend_WhenStatusIsDeliveryAttemptFailed_SendsDeliveryAttemptFailedToCommerce()
    {
        using var db = CreateDb();
        var log = await AddPendingAsync(db, status: "DeliveryAttemptFailed");
        var commerce = new FakeCommerce();

        var delivered = await CreateOutbox(db, commerce).TrySendAsync(log);

        delivered.Should().BeTrue();
        var sent = commerce.Sent.Should().ContainSingle().Subject;
        sent.Status.Should().Be("DeliveryAttemptFailed");
    }

    [Fact]
    public async Task TrySend_WhenCommerceDoesNotAnswer_KeepsItQueuedWithBackoff()
    {
        using var db = CreateDb();
        var log = await AddPendingAsync(db);
        var before = DateTimeOffset.UtcNow;

        var delivered = await CreateOutbox(db, new FakeCommerce { Succeeds = false }).TrySendAsync(log);

        delivered.Should().BeFalse();
        log.CommerceNotifiedAt.Should().BeNull();
        log.CommerceNotifyAttempts.Should().Be(1);
        log.CommerceNotifyNextRetryAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task TrySend_WhenTheClientThrows_IsTreatedAsNotDelivered()
    {
        using var db = CreateDb();
        var log = await AddPendingAsync(db);

        var delivered = await CreateOutbox(db, new FakeCommerce { Throws = true }).TrySendAsync(log);

        delivered.Should().BeFalse();
        log.CommerceNotifyAttempts.Should().Be(1);
    }

    [Fact]
    public async Task ProcessDue_RetriesUntilCommerceIsBackThenStops()
    {
        using var db = CreateDb();
        var log = await AddPendingAsync(db);
        var commerce = new FakeCommerce { Succeeds = false };
        var outbox = CreateOutbox(db, commerce);

        await outbox.TrySendAsync(log);                        // Commerce is down
        log.CommerceNotifiedAt.Should().BeNull();

        commerce.Succeeds = true;                              // Commerce is back and the retry time has come
        log.CommerceNotifyNextRetryAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();
        (await outbox.ProcessDueAsync(10)).Should().Be(1);
        log.CommerceNotifiedAt.Should().NotBeNull();

        (await outbox.ProcessDueAsync(10)).Should().Be(0);     // nothing left to send
        commerce.Sent.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProcessDue_SkipsUpdatesThatAreNotDueSentOrExhausted()
    {
        using var db = CreateDb();
        var notDue = await AddPendingAsync(db, nextRetry: DateTimeOffset.UtcNow.AddMinutes(10));
        var alreadySent = await AddPendingAsync(db, notifiedAt: DateTimeOffset.UtcNow.AddMinutes(-5));
        var exhausted = await AddPendingAsync(db, attempts: DeliveryNotificationOutbox.MaxAttempts);
        var commerce = new FakeCommerce();

        (await CreateOutbox(db, commerce).ProcessDueAsync(10)).Should().Be(0);

        commerce.Sent.Should().BeEmpty();
        notDue.CommerceNotifiedAt.Should().BeNull();
        exhausted.CommerceNotifiedAt.Should().BeNull();
        alreadySent.CommerceNotifyAttempts.Should().Be(0);
    }

    [Fact]
    public async Task ProcessDue_SendsTheOldestUpdateFirst()
    {
        using var db = CreateDb();
        var order = Guid.NewGuid();
        var first = await AddPendingAsync(db, orderId: order, status: "InTransit", receivedAt: DateTimeOffset.UtcNow.AddMinutes(-3));
        var second = await AddPendingAsync(db, orderId: order, status: "Delivered", receivedAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        var commerce = new FakeCommerce();

        await CreateOutbox(db, commerce).ProcessDueAsync(10);

        commerce.Sent.Select(s => s.Status).Should().Equal("InTransit", "Delivered");
    }

    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static DeliveryNotificationOutbox CreateOutbox(AppDbContext db, ICommerceWebhookClient commerce) =>
        new(db, commerce, NullLogger<DeliveryNotificationOutbox>.Instance);

    private static async Task<WebhookAuditLog> AddPendingAsync(
        AppDbContext db, Guid? orderId = null, string status = "Delivered", DateTimeOffset? receivedAt = null,
        DateTimeOffset? nextRetry = null, DateTimeOffset? notifiedAt = null, int attempts = 0)
    {
        var log = new WebhookAuditLog
        {
            Id = Guid.NewGuid(), OrderCode = "GHN123", Status = status.ToLowerInvariant(), IsProcessed = true,
            ReceivedAt = receivedAt ?? DateTimeOffset.UtcNow.AddMinutes(-2),
            NotifyOrderId = orderId ?? Guid.NewGuid(), NotifyStatus = status, NotifyTrackingNumber = "GHN123", NotifyDescription = "ok",
            CommerceNotifyAttempts = attempts, CommerceNotifyNextRetryAt = nextRetry, CommerceNotifiedAt = notifiedAt
        };
        db.WebhookAuditLogs.Add(log);
        await db.SaveChangesAsync();
        return log;
    }

    private sealed class FakeCommerce : ICommerceWebhookClient
    {
        public bool Succeeds { get; set; } = true;
        public bool Throws { get; set; }
        public List<(Guid OrderId, string Status, DateTimeOffset? OccurredAt)> Sent { get; } = new();

        public Task<bool> NotifyDeliveryStatusAsync(
            Guid subOrderId, string status, string? trackingNumber = null, string? description = null,
            DateTimeOffset? occurredAt = null, CancellationToken cancellationToken = default)
        {
            Sent.Add((subOrderId, status, occurredAt));
            if (Throws)
                throw new HttpRequestException("boom");
            return Task.FromResult(Succeeds);
        }
    }
}

public sealed class CommerceWebhookClientContractTests
{
    [Fact]
    public async Task Notify_SendsCommercesFieldNames_AndTheOriginalEventTime()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK);
        var client = CreateClient(handler);
        var happened = new DateTimeOffset(2026, 11, 16, 18, 47, 1, TimeSpan.Zero);

        var acknowledged = await client.NotifyDeliveryStatusAsync(Guid.NewGuid(), "Delivered", "GHN123", "left at the door", happened);

        acknowledged.Should().BeTrue();
        var body = JsonDocument.Parse(handler.Body!).RootElement;
        body.GetProperty("status").GetString().Should().Be("Delivered");
        body.GetProperty("trackingCode").GetString().Should().Be("GHN123");
        body.GetProperty("note").GetString().Should().Be("left at the door");
        body.GetProperty("carrierCode").GetString().Should().Be("GHN");
        body.GetProperty("deliveredAt").GetDateTimeOffset().Should().Be(happened);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Notify_ReportsFailureWhenCommerceDoesNotAcknowledge(HttpStatusCode status)
    {
        var client = CreateClient(new CapturingHandler(status));

        (await client.NotifyDeliveryStatusAsync(Guid.NewGuid(), "Delivered")).Should().BeFalse();
    }

    private static CommerceWebhookClient CreateClient(HttpMessageHandler handler) =>
        new(new ConfigurationBuilder().Build(), NullLogger<CommerceWebhookClient>.Instance,
            new HttpClient(handler) { BaseAddress = new Uri("http://commerce.test/") });

    private sealed class CapturingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent("{}") };
        }
    }
}
