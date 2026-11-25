namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Infrastructure.Carriers.Ghn;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class GhnWebhookComponentTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void GhnStatusMapper_MapsAllCarrierMilestonesAccurately()
    {
        GhnStatusMapper.Map("ready_to_pick").Should().Be(ShipmentStatus.ReadyToPick);
        GhnStatusMapper.Map("picking").Should().Be(ShipmentStatus.Picking);
        GhnStatusMapper.Map("picked").Should().Be(ShipmentStatus.HandedOver);
        GhnStatusMapper.Map("delivering").Should().Be(ShipmentStatus.InTransit);
        GhnStatusMapper.Map("sorting").Should().Be(ShipmentStatus.InTransit);
        GhnStatusMapper.Map("transporting").Should().Be(ShipmentStatus.InTransit);
        GhnStatusMapper.Map("delivered").Should().Be(ShipmentStatus.Delivered);
        GhnStatusMapper.Map("delivery_fail").Should().Be(ShipmentStatus.DeliveryFailed);
        GhnStatusMapper.Map("returning").Should().Be(ShipmentStatus.Returning);
        GhnStatusMapper.Map("waiting_to_return").Should().Be(ShipmentStatus.Returning);
        GhnStatusMapper.Map("returned").Should().Be(ShipmentStatus.Returned);
        GhnStatusMapper.Map("cancel").Should().Be(ShipmentStatus.Cancelled);
        GhnStatusMapper.Map("lost").Should().Be(ShipmentStatus.DeliveryFailed);
        GhnStatusMapper.Map("damage").Should().Be(ShipmentStatus.DeliveryFailed);
    }

    [Fact]
    public async Task DeliveryNotificationOutbox_ExtractsAllCarrierMetadata_FromRawJson()
    {
        using var db = CreateDb();
        var fakeCommerce = new RecordingCommerceClient();
        var outbox = new DeliveryNotificationOutbox(db, fakeCommerce, NullLogger<DeliveryNotificationOutbox>.Instance);

        var orderId = Guid.NewGuid();
        var rawJson = JsonSerializer.Serialize(new
        {
            ShipperName = "Anh Shipper",
            ShipperPhone = "0909999888",
            PodUrl = "https://cdn.ghn.vn/pod123.jpg",
            ReasonCode = "REASON_CUSTOMER_UNREACHABLE",
            Reason = "Gọi 3 cuộc không nghe máy",
            Type = "switch_status",
            Time = "2026-11-25T14:00:00Z"
        });

        var log = new WebhookAuditLog
        {
            Id = Guid.NewGuid(),
            OrderCode = "GHN-TEST-META",
            Status = "delivered",
            NotifyOrderId = orderId,
            NotifyStatus = "Delivered",
            NotifyTrackingNumber = "GHN-TEST-META",
            RawJson = rawJson,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsProcessed = true
        };

        var delivered = await outbox.TrySendAsync(log);

        delivered.Should().BeTrue();
        fakeCommerce.SentRequests.Should().HaveCount(1);

        var sent = fakeCommerce.SentRequests[0];
        sent.SubOrderId.Should().Be(orderId);
        sent.ShipperName.Should().Be("Anh Shipper");
        sent.ShipperPhone.Should().Be("0909999888");
        sent.PodUrl.Should().Be("https://cdn.ghn.vn/pod123.jpg");
        sent.ReasonCode.Should().Be("REASON_CUSTOMER_UNREACHABLE");
        sent.Reason.Should().Be("Gọi 3 cuộc không nghe máy");
        sent.EventType.Should().Be("switch_status");
        sent.EventTime.Should().Be(DateTimeOffset.Parse("2026-11-25T14:00:00Z"));
    }

    private sealed class RecordingCommerceClient : ICommerceWebhookClient
    {
        public List<CommerceDeliveryStatusRequest> SentRequests { get; } = new();

        public Task<bool> NotifyDeliveryStatusAsync(
            Guid subOrderId,
            string status,
            string? trackingNumber = null,
            string? description = null,
            DateTimeOffset? occurredAt = null,
            CancellationToken cancellationToken = default)
        {
            SentRequests.Add(new CommerceDeliveryStatusRequest
            {
                SubOrderId = subOrderId,
                Status = status,
                TrackingCode = trackingNumber,
                Note = description,
                DeliveredAt = occurredAt
            });
            return Task.FromResult(true);
        }

        public Task<bool> NotifyDeliveryStatusAsync(CommerceDeliveryStatusRequest request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return Task.FromResult(true);
        }
    }
}
