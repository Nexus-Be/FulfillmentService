namespace Fulfillment.UnitTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Fulfillment.Application.Exceptions;
using Fulfillment.Application.Interfaces;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class SimulateShipperStatusTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class FakeCarrierGateway : ICarrierGateway
    {
        public string Code => "GHN";
        public Task<CarrierFeeCalculationResult> CalculateFeeAsync(CarrierFeeCalculationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CarrierFeeCalculationResult(28000m, DateTimeOffset.UtcNow.AddDays(2), "GHN_STANDARD"));
        public Task<CarrierCreateShipmentResult> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CarrierCreateShipmentResult("GHN_TEST", 28000m, DateTimeOffset.UtcNow.AddDays(2), ShipmentStatus.ReadyToPick, "https://labels.example.com"));
        public Task<CarrierStatusResult> GetStatusAsync(string trackingCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CarrierStatusResult(trackingCode, ShipmentStatus.InTransit, "delivering", "In delivery", DateTimeOffset.UtcNow));
        public Task<CarrierCancelResult> CancelAsync(string trackingCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CarrierCancelResult(trackingCode, true, "Success"));
        public Task<string?> GetPrintTokenAsync(string trackingCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>("mock_token");
        public ShipmentStatus MapStatus(string rawCarrierStatus) => ShipmentStatus.ReadyToPick;
    }

    private sealed class FakeCarrierResolver : ICarrierResolver
    {
        private readonly ICarrierGateway _gateway;
        public FakeCarrierResolver(ICarrierGateway gateway) => _gateway = gateway;
        public ICarrierGateway Get(string? carrierCode = null) => _gateway;
    }

    private sealed class FakeCommerceWebhookClient : ICommerceWebhookClient
    {
        public Guid? LastOrderId { get; private set; }
        public string? LastStatus { get; private set; }
        public string? LastTrackingNumber { get; private set; }

        public Task<bool> NotifyDeliveryStatusAsync(
            Guid subOrderId,
            string status,
            string? trackingNumber = null,
            string? description = null,
            DateTimeOffset? occurredAt = null,
            CancellationToken cancellationToken = default)
        {
            LastOrderId = subOrderId;
            LastStatus = status;
            LastTrackingNumber = trackingNumber;
            return Task.FromResult(true);
        }

        public Task<bool> NotifyDeliveryStatusAsync(
            CommerceDeliveryStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            LastOrderId = request.SubOrderId;
            LastStatus = request.Status;
            LastTrackingNumber = request.TrackingCode;
            return Task.FromResult(true);
        }
    }

    private sealed class FakeNotificationClient : INotificationClient
    {
        public DeliveryAlertRequest? LastAlert { get; private set; }

        public Task<bool> SendShipmentStatusEmailAsync(
            string recipientEmail,
            string trackingNumber,
            string statusText,
            CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> SendDeliveryAlertAsync(
            DeliveryAlertRequest request,
            CancellationToken cancellationToken = default)
        {
            LastAlert = request;
            return Task.FromResult(true);
        }
    }

    private async Task<Shipment> SeedShipmentAsync(AppDbContext db, string trackingNumber, ShipmentStatus status = ShipmentStatus.ReadyToPick)
    {
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Code = "WH01",
            Name = "Main Warehouse",
            IsActive = true
        };
        db.Warehouses.Add(warehouse);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            WarehouseId = warehouse.Id,
            CarrierId = Guid.NewGuid(),
            TrackingNumber = trackingNumber,
            GhnOrderCode = trackingNumber,
            Status = status,
            ReceiverName = "Nguyen Van A",
            ReceiverPhone = "0901234567",
            ReceiverAddress = "123 Le Loi, Q1, HCMC"
        };
        db.Shipments.Add(shipment);
        await db.SaveChangesAsync();
        return shipment;
    }

    [Fact]
    public async Task SimulateShipperStatus_Delivering_TransitionsToInTransit_AndQueuesCommerceNotification()
    {
        // Arrange
        using var db = CreateDbContext();
        var tracking = "GHN_SIM_001";
        var shipment = await SeedShipmentAsync(db, tracking, ShipmentStatus.ReadyToPick);

        var fakeCarrier = new FakeCarrierGateway();
        var resolver = new FakeCarrierResolver(fakeCarrier);
        var commerce = new FakeCommerceWebhookClient();
        var outbox = new DeliveryNotificationOutbox(db, commerce, new Microsoft.Extensions.Logging.Abstractions.NullLogger<DeliveryNotificationOutbox>());
        var notif = new FakeNotificationClient();

        var service = new ShipmentService(db, resolver, outbox: outbox, notificationClient: notif);

        var request = new SimulateShipperStatusRequest(
            TrackingNumber: tracking,
            Status: "delivering",
            Description: "Parcel picked up and on route to recipient",
            ShipperName: "Tran Van Shipper",
            ShipperPhone: "0987654321");

        // Act
        var response = await service.SimulateShipperStatusAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.IsSuccess.Should().BeTrue();
        response.PreviousStatus.Should().Be("ReadyToPick");
        response.CurrentStatus.Should().Be("InTransit");
        response.CommerceStatusNotified.Should().Be("InTransit");

        // Verify Database
        var updated = await db.Shipments.Include(s => s.Events).FirstAsync(s => s.TrackingNumber == tracking);
        updated.Status.Should().Be(ShipmentStatus.InTransit);
        updated.Events.Should().ContainSingle(e => e.CarrierStatus == "delivering" && e.ShipperName == "Tran Van Shipper");

        // Verify Commerce Notification
        commerce.LastStatus.Should().Be("InTransit");
        commerce.LastOrderId.Should().Be(shipment.OrderId);

        // Verify Notification Client
        notif.LastAlert.Should().NotBeNull();
        notif.LastAlert!.Status.Should().Be("InTransit");
        notif.LastAlert.ShipperName.Should().Be("Tran Van Shipper");
    }

    [Fact]
    public async Task SimulateShipperStatus_Delivered_SetsDeliveredAt_AndNotifiesCommerce()
    {
        // Arrange
        using var db = CreateDbContext();
        var tracking = "GHN_SIM_002";
        var shipment = await SeedShipmentAsync(db, tracking, ShipmentStatus.InTransit);

        var fakeCarrier = new FakeCarrierGateway();
        var resolver = new FakeCarrierResolver(fakeCarrier);
        var commerce = new FakeCommerceWebhookClient();
        var outbox = new DeliveryNotificationOutbox(db, commerce, new Microsoft.Extensions.Logging.Abstractions.NullLogger<DeliveryNotificationOutbox>());

        var service = new ShipmentService(db, resolver, outbox: outbox);

        var request = new SimulateShipperStatusRequest(
            TrackingNumber: tracking,
            Status: "delivered",
            Description: "Successful delivery with signature",
            PodUrl: "https://cdn.example.com/pod.jpg");

        // Act
        var response = await service.SimulateShipperStatusAsync(request);

        // Assert
        response.CurrentStatus.Should().Be("Delivered");
        response.CommerceStatusNotified.Should().Be("Delivered");

        var updated = await db.Shipments.FirstAsync(s => s.TrackingNumber == tracking);
        updated.Status.Should().Be(ShipmentStatus.Delivered);
        updated.DeliveredAt.Should().NotBeNull();

        commerce.LastStatus.Should().Be("Delivered");
    }

    [Fact]
    public async Task SimulateShipperStatus_DeliveryFail_MapsToDeliveryAttemptFailed_ForCommerce()
    {
        // Arrange
        using var db = CreateDbContext();
        var tracking = "GHN_SIM_003";
        var shipment = await SeedShipmentAsync(db, tracking, ShipmentStatus.InTransit);

        var fakeCarrier = new FakeCarrierGateway();
        var resolver = new FakeCarrierResolver(fakeCarrier);
        var commerce = new FakeCommerceWebhookClient();
        var outbox = new DeliveryNotificationOutbox(db, commerce, new Microsoft.Extensions.Logging.Abstractions.NullLogger<DeliveryNotificationOutbox>());

        var service = new ShipmentService(db, resolver, outbox: outbox);

        var request = new SimulateShipperStatusRequest(
            TrackingNumber: tracking,
            Status: "delivery_fail",
            ReasonCode: "BUYER_NOT_HOME",
            Reason: "Buyer could not be reached via telephone");

        // Act
        var response = await service.SimulateShipperStatusAsync(request);

        // Assert
        response.CurrentStatus.Should().Be("DeliveryFailed");
        // Rule: delivery_fail maps to DeliveryAttemptFailed when notifying Commerce!
        response.CommerceStatusNotified.Should().Be("DeliveryAttemptFailed");
        commerce.LastStatus.Should().Be("DeliveryAttemptFailed");

        var updated = await db.Shipments.FirstAsync(s => s.TrackingNumber == tracking);
        updated.Status.Should().Be(ShipmentStatus.DeliveryFailed);
    }

    [Fact]
    public async Task SimulateShipperStatus_InvalidBackwardTransition_WithoutForce_ThrowsException()
    {
        // Arrange
        using var db = CreateDbContext();
        var tracking = "GHN_SIM_004";
        await SeedShipmentAsync(db, tracking, ShipmentStatus.Delivered);

        var service = new ShipmentService(db, new FakeCarrierResolver(new FakeCarrierGateway()));

        var request = new SimulateShipperStatusRequest(
            TrackingNumber: tracking,
            Status: "delivering",
            Force: false);

        // Act & Assert
        var act = async () => await service.SimulateShipperStatusAsync(request);
        var ex = await act.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.ErrorCode.Should().Be("INVALID_STATUS_TRANSITION");
    }

    [Fact]
    public async Task SimulateShipperStatus_InvalidBackwardTransition_WithForce_Succeeds()
    {
        // Arrange
        using var db = CreateDbContext();
        var tracking = "GHN_SIM_005";
        await SeedShipmentAsync(db, tracking, ShipmentStatus.Delivered);

        var service = new ShipmentService(db, new FakeCarrierResolver(new FakeCarrierGateway()));

        var request = new SimulateShipperStatusRequest(
            TrackingNumber: tracking,
            Status: "InTransit",
            Force: true);

        // Act
        var response = await service.SimulateShipperStatusAsync(request);

        // Assert
        response.CurrentStatus.Should().Be("InTransit");
        var updated = await db.Shipments.FirstAsync(s => s.TrackingNumber == tracking);
        updated.Status.Should().Be(ShipmentStatus.InTransit);
    }

    [Fact]
    public async Task SimulateShipperStatus_NonExistentTrackingNumber_ThrowsEntityNotFound()
    {
        // Arrange
        using var db = CreateDbContext();
        var service = new ShipmentService(db, new FakeCarrierResolver(new FakeCarrierGateway()));

        var request = new SimulateShipperStatusRequest(
            TrackingNumber: "NON_EXISTENT_TRACKING",
            Status: "delivered");

        // Act & Assert
        var act = async () => await service.SimulateShipperStatusAsync(request);
        var ex = await act.Should().ThrowAsync<EntityNotFoundException>();
        ex.Which.ErrorCode.Should().Be("SHIPMENT_NOT_FOUND");
    }

    [Fact]
    public async Task SimulateShipperStatus_UnrecognizedStatus_ThrowsValidationException()
    {
        // Arrange
        using var db = CreateDbContext();
        var tracking = "GHN_SIM_006";
        await SeedShipmentAsync(db, tracking, ShipmentStatus.ReadyToPick);

        var service = new ShipmentService(db, new FakeCarrierResolver(new FakeCarrierGateway()));

        var request = new SimulateShipperStatusRequest(
            TrackingNumber: tracking,
            Status: "some_weird_unknown_status");

        // Act & Assert
        var act = async () => await service.SimulateShipperStatusAsync(request);
        var ex = await act.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.ErrorCode.Should().Be("VAL_INVALID_STATUS");
    }
}
