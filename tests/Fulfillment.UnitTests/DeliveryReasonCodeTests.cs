namespace Fulfillment.UnitTests;

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Domain.Entities;
using Fulfillment.Infrastructure.HttpClients;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>The carrier's reason for a failed delivery reaches Commerce exactly as reported, and a retry sends the same thing.</summary>
public sealed class DeliveryReasonCodeTests
{
    private sealed class CapturingCommerce : ICommerceWebhookClient
    {
        public bool Succeeds { get; set; } = true;
        public List<CommerceDeliveryStatusRequest> Requests { get; } = new();

        public Task<bool> NotifyDeliveryStatusAsync(
            Guid subOrderId, string status, string? trackingNumber = null, string? description = null,
            DateTimeOffset? occurredAt = null, CancellationToken cancellationToken = default) => Task.FromResult(Succeeds);

        public Task<bool> NotifyDeliveryStatusAsync(CommerceDeliveryStatusRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(Succeeds);
        }
    }

    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<WebhookAuditLog> AddFailureAsync(AppDbContext db, string? rawJson, DateTimeOffset? nextRetry = null)
    {
        var log = new WebhookAuditLog
        {
            Id = Guid.NewGuid(), OrderCode = "GHN123", Status = "delivery_fail", IsProcessed = true, RawJson = rawJson,
            ReceivedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            NotifyOrderId = Guid.NewGuid(), NotifyStatus = "DeliveryFailed", NotifyTrackingNumber = "GHN123", NotifyDescription = "Delivery failed",
            CommerceNotifyAttempts = 0, CommerceNotifyNextRetryAt = nextRetry
        };
        db.WebhookAuditLogs.Add(log);
        await db.SaveChangesAsync();
        return log;
    }

    private static DeliveryNotificationOutbox Outbox(AppDbContext db, ICommerceWebhookClient commerce) =>
        new(db, commerce, NullLogger<DeliveryNotificationOutbox>.Instance);

    [Fact]
    public async Task The_reason_code_and_text_in_the_carrier_payload_are_sent_unchanged()
    {
        using var db = CreateDb();
        var commerce = new CapturingCommerce();
        var log = await AddFailureAsync(db, """{"OrderCode":"GHN123","Status":"delivery_fail","ReasonCode":" 102 ","Reason":"Customer refused to receive"}""");

        await Outbox(db, commerce).TrySendAsync(log);

        var sent = commerce.Requests.Should().ContainSingle().Subject;
        sent.ReasonCode.Should().Be("102");
        sent.Reason.Should().Be("Customer refused to receive");
    }

    [Fact]
    public async Task The_carriers_own_event_type_and_time_are_sent_not_the_time_of_receipt()
    {
        using var db = CreateDb();
        var commerce = new CapturingCommerce();
        var log = await AddFailureAsync(db, """{"OrderCode":"GHN123","Status":"delivery_fail","Type":"switch_status","Time":"2026-11-25T03:00:00.000Z","ReasonCode":"GHN-DCD0A8"}""");

        await Outbox(db, commerce).TrySendAsync(log);

        var sent = commerce.Requests.Should().ContainSingle().Subject;
        sent.EventType.Should().Be("switch_status");
        sent.EventTime.Should().Be(new DateTimeOffset(2026, 11, 25, 3, 0, 0, TimeSpan.Zero));
        sent.EventTime.Should().NotBe(log.ReceivedAt);
    }

    [Theory]
    [InlineData("""{"OrderCode":"GHN123","Status":"delivery_fail"}""")]
    [InlineData("""{"OrderCode":"GHN123","Status":"delivery_fail","Type":null,"Time":null}""")]
    [InlineData("""{"OrderCode":"GHN123","Status":"delivery_fail","Time":"not a time"}""")]
    [InlineData("""{"OrderCode":"GHN123","Status":"delivery_fail","Time":12345}""")]
    public async Task A_missing_or_unreadable_event_time_is_sent_as_nothing_never_replaced_by_the_time_of_receipt(string raw)
    {
        using var db = CreateDb();
        var commerce = new CapturingCommerce();
        var log = await AddFailureAsync(db, raw);

        await Outbox(db, commerce).TrySendAsync(log);

        commerce.Requests.Should().ContainSingle().Subject.EventTime.Should().BeNull();
    }

    [Theory]
    [InlineData("""{"OrderCode":"GHN123","Status":"returned"}""")]
    [InlineData("""{"OrderCode":"GHN123","Status":"returned","ReasonCode":null,"Reason":null}""")]
    [InlineData("""{"OrderCode":"GHN123","Status":"returned","ReasonCode":"  ","Reason":""}""")]
    [InlineData("not json at all")]
    [InlineData(null)]
    public async Task A_missing_or_empty_reason_is_sent_as_nothing_never_invented(string? raw)
    {
        using var db = CreateDb();
        var commerce = new CapturingCommerce();
        var log = await AddFailureAsync(db, raw);

        await Outbox(db, commerce).TrySendAsync(log);

        var sent = commerce.Requests.Should().ContainSingle().Subject;
        sent.ReasonCode.Should().BeNull();
        sent.Reason.Should().BeNull();
    }

    [Fact]
    public async Task A_retry_after_a_failed_send_carries_the_same_reason()
    {
        using var db = CreateDb();
        var commerce = new CapturingCommerce { Succeeds = false };
        var log = await AddFailureAsync(db, """{"ReasonCode":"102","Reason":"Customer refused"}""", nextRetry: DateTimeOffset.UtcNow.AddMinutes(-1));

        await Outbox(db, commerce).TrySendAsync(log);
        commerce.Succeeds = true;
        log.CommerceNotifyNextRetryAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();
        await Outbox(db, commerce).ProcessDueAsync(10);

        commerce.Requests.Should().HaveCount(2);
        commerce.Requests.Select(item => item.ReasonCode).Should().Equal("102", "102");
        commerce.Requests.Select(item => item.Reason).Should().Equal("Customer refused", "Customer refused");
    }

    [Fact]
    public async Task The_http_payload_to_commerce_names_the_reason_fields()
    {
        var handler = new CaptureHandler();
        var client = new CommerceWebhookClient(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["InternalSecurity:ServiceKey"] = "k" }).Build(),
            NullLogger<CommerceWebhookClient>.Instance,
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8003/") });

        await client.NotifyDeliveryStatusAsync(new CommerceDeliveryStatusRequest
        {
            SubOrderId = Guid.NewGuid(), Status = "DeliveryFailed", ReasonCode = "102", Reason = "Customer refused",
            EventType = "switch_status", EventTime = new DateTimeOffset(2026, 11, 25, 3, 0, 0, TimeSpan.Zero)
        });

        using var doc = JsonDocument.Parse(handler.Body!);
        doc.RootElement.GetProperty("reasonCode").GetString().Should().Be("102");
        doc.RootElement.GetProperty("reason").GetString().Should().Be("Customer refused");
        doc.RootElement.GetProperty("eventType").GetString().Should().Be("switch_status");
        doc.RootElement.GetProperty("eventTime").GetDateTimeOffset().Should().Be(new DateTimeOffset(2026, 11, 25, 3, 0, 0, TimeSpan.Zero));
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
