namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Fulfillment.Application.Exceptions;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ShipmentAdvancedTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class MockCarrierGateway : ICarrierGateway
    {
        public string Code => "GHN";
        public bool CancelCalled { get; private set; }
        public bool PrintTokenCalled { get; private set; }

        public Task<CarrierFeeCalculationResult> CalculateFeeAsync(CarrierFeeCalculationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new CarrierFeeCalculationResult(25000m));

        public Task<CarrierCreateShipmentResult> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new CarrierCreateShipmentResult("GHN_TEST_001", 25000m, DateTimeOffset.UtcNow.AddDays(2), ShipmentStatus.ReadyToPick));

        public Task<CarrierStatusResult> GetStatusAsync(string trackingCode, CancellationToken cancellationToken = default)
            => Task.FromResult(new CarrierStatusResult(trackingCode, ShipmentStatus.ReadyToPick, "ready_to_pick"));

        public Task<CarrierCancelResult> CancelAsync(string trackingCode, CancellationToken cancellationToken = default)
        {
            CancelCalled = true;
            return Task.FromResult(new CarrierCancelResult(trackingCode, true, "Cancelled by test"));
        }

        public Task<string?> GetPrintTokenAsync(string trackingCode, CancellationToken cancellationToken = default)
        {
            PrintTokenCalled = true;
            return Task.FromResult<string?>("mock_ghn_token_xyz");
        }

        public ShipmentStatus MapStatus(string rawCarrierStatus) => ShipmentStatus.ReadyToPick;
    }

    private sealed class MockCarrierResolver : ICarrierResolver
    {
        private readonly ICarrierGateway _carrier;
        public MockCarrierResolver(ICarrierGateway carrier) => _carrier = carrier;
        public ICarrierGateway Get(string? carrierCode = null) => _carrier;
    }

    [Fact]
    public async Task ListShipmentsAsync_ShouldEnforceSellerIsolation_AndFilterCorrectly()
    {
        // Arrange
        using var db = CreateDbContext();
        var seller1 = Guid.NewGuid();
        var seller2 = Guid.NewGuid();

        var wh1 = new Warehouse { Id = Guid.NewGuid(), SellerId = seller1, Code = "WH1", Name = "Warehouse 1", Address = "HCM" };
        var wh2 = new Warehouse { Id = Guid.NewGuid(), SellerId = seller2, Code = "WH2", Name = "Warehouse 2", Address = "HN" };
        db.Warehouses.AddRange(wh1, wh2);

        var s1 = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            WarehouseId = wh1.Id,
            CarrierId = Guid.NewGuid(),
            TrackingNumber = "GHN_TRACK_S1",
            Status = ShipmentStatus.ReadyToPick,
            ReceiverName = "Buyer 1",
            ReceiverPhone = "0900000001",
            ReceiverAddress = "123 Le Loi",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-2)
        };

        var s2 = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            WarehouseId = wh2.Id,
            CarrierId = Guid.NewGuid(),
            TrackingNumber = "GHN_TRACK_S2",
            Status = ShipmentStatus.InTransit,
            ReceiverName = "Buyer 2",
            ReceiverPhone = "0900000002",
            ReceiverAddress = "456 Tran Phu",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        db.Shipments.AddRange(s1, s2);
        await db.SaveChangesAsync();

        var carrier = new MockCarrierGateway();
        var resolver = new MockCarrierResolver(carrier);
        var service = new ShipmentService(db, resolver);

        // Act - Seller 1 accesses: only sees s1
        var filter = new ShipmentFilterRequest();
        var seller1Result = await service.ListShipmentsAsync(filter, seller1, isAdmin: false);

        // Assert
        seller1Result.Items.Should().HaveCount(1);
        seller1Result.Items[0].TrackingNumber.Should().Be("GHN_TRACK_S1");
        seller1Result.Pagination.TotalCount.Should().Be(1);

        // Act - Admin accesses: sees both s1 and s2
        var adminResult = await service.ListShipmentsAsync(filter, Guid.NewGuid(), isAdmin: true);
        adminResult.Items.Should().HaveCount(2);
        adminResult.Pagination.TotalCount.Should().Be(2);

        // Act - Filter by status
        var statusFilter = new ShipmentFilterRequest(Status: "ReadyToPick");
        var filteredResult = await service.ListShipmentsAsync(statusFilter, Guid.NewGuid(), isAdmin: true);
        filteredResult.Items.Should().HaveCount(1);
        filteredResult.Items[0].TrackingNumber.Should().Be("GHN_TRACK_S1");
    }

    [Fact]
    public async Task CancelShipmentAsync_ShouldSyncWithCarrier_AndPersistCancelledEvent()
    {
        // Arrange
        using var db = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var wh = new Warehouse { Id = Guid.NewGuid(), SellerId = sellerId, Code = "WH1", Name = "Warehouse 1", Address = "HCM" };
        var carrierEntity = new Carrier { Id = Guid.NewGuid(), Code = "GHN", Name = "Giao Hang Nhanh", Priority = 1, IsActive = true };
        db.Warehouses.Add(wh);
        db.Carriers.Add(carrierEntity);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            WarehouseId = wh.Id,
            CarrierId = carrierEntity.Id,
            TrackingNumber = "GHN_CANCEL_TEST",
            Status = ShipmentStatus.ReadyToPick,
            ReceiverName = "Buyer",
            ReceiverPhone = "0900000000",
            ReceiverAddress = "Address"
        };
        db.Shipments.Add(shipment);
        await db.SaveChangesAsync();

        var carrier = new MockCarrierGateway();
        var resolver = new MockCarrierResolver(carrier);
        var service = new ShipmentService(db, resolver);

        // Act
        var result = await service.CancelShipmentAsync(shipment.Id, "Customer requested cancellation", sellerId, isAdmin: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TrackingNumber.Should().Be("GHN_CANCEL_TEST");
        carrier.CancelCalled.Should().BeTrue();

        var updatedShipment = await db.Shipments.Include(s => s.Events).FirstOrDefaultAsync(s => s.Id == shipment.Id);
        updatedShipment.Should().NotBeNull();
        updatedShipment!.Status.Should().Be(ShipmentStatus.Cancelled);
        updatedShipment.Events.Should().Contain(e => e.Status == ShipmentEventStatus.Cancelled && e.Reason == "Customer requested cancellation");
    }

    [Fact]
    public async Task CancelShipmentAsync_ShouldThrowForbidden_WhenUserIsNotOwner()
    {
        // Arrange
        using var db = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var wh = new Warehouse { Id = Guid.NewGuid(), SellerId = sellerId, Code = "WH1", Name = "Warehouse 1", Address = "HCM" };
        var carrierEntity = new Carrier { Id = Guid.NewGuid(), Code = "GHN", Name = "Giao Hang Nhanh", Priority = 1, IsActive = true };
        db.Warehouses.Add(wh);
        db.Carriers.Add(carrierEntity);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            WarehouseId = wh.Id,
            CarrierId = carrierEntity.Id,
            TrackingNumber = "GHN_STRANGER_TEST",
            Status = ShipmentStatus.ReadyToPick,
            ReceiverName = "Buyer",
            ReceiverPhone = "0900000000",
            ReceiverAddress = "Address"
        };
        db.Shipments.Add(shipment);
        await db.SaveChangesAsync();

        var carrier = new MockCarrierGateway();
        var resolver = new MockCarrierResolver(carrier);
        var service = new ShipmentService(db, resolver);

        // Act & Assert
        var act = async () => await service.CancelShipmentAsync(shipment.Id, "Cancel attempt", strangerId, isAdmin: false);
        await act.Should().ThrowAsync<BusinessRuleValidationException>()
            .WithMessage("*do not have permission*");
    }

    [Fact]
    public async Task GetPrintLabelAsync_ShouldCallGateway_AndGenerateCorrectThermalPrintUrl()
    {
        // Arrange
        using var db = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var wh = new Warehouse { Id = Guid.NewGuid(), SellerId = sellerId, Code = "WH1", Name = "Warehouse 1", Address = "HCM" };
        var carrierEntity = new Carrier { Id = Guid.NewGuid(), Code = "GHN", Name = "Giao Hang Nhanh", Priority = 1, IsActive = true };
        db.Warehouses.Add(wh);
        db.Carriers.Add(carrierEntity);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            WarehouseId = wh.Id,
            CarrierId = carrierEntity.Id,
            TrackingNumber = "GHN_PRINT_TEST",
            Status = ShipmentStatus.ReadyToPick,
            ReceiverName = "Buyer",
            ReceiverPhone = "0900000000",
            ReceiverAddress = "Address"
        };
        db.Shipments.Add(shipment);
        await db.SaveChangesAsync();

        var carrier = new MockCarrierGateway();
        var resolver = new MockCarrierResolver(carrier);
        var service = new ShipmentService(db, resolver);

        // Act - Request 80x80 label
        var result = await service.GetPrintLabelAsync(shipment.Id, "80x80", sellerId, isAdmin: false);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("mock_ghn_token_xyz");
        result.PageSize.Should().Be("80x80");
        result.PrintUrl.Should().Be("https://dev-online-gateway.ghn.vn/a5/public-api/print80x80?token=mock_ghn_token_xyz");
        carrier.PrintTokenCalled.Should().BeTrue();

        var updated = await db.Shipments.FindAsync(shipment.Id);
        updated!.PrintToken.Should().Be("mock_ghn_token_xyz");
    }

    [Fact]
    public async Task ListShipmentsAsync_ShouldSupportUnifiedSearch_MultiStatusFilter_AndTimeSorting()
    {
        using var db = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var wh = new Warehouse { Id = Guid.NewGuid(), SellerId = sellerId, Code = "WH1", Name = "Warehouse 1", Address = "HCM" };
        db.Warehouses.Add(wh);

        var t0 = DateTimeOffset.UtcNow.AddHours(-5);
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        var orderId3 = Guid.NewGuid();

        var s1 = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId1,
            WarehouseId = wh.Id,
            CarrierId = Guid.NewGuid(),
            TrackingNumber = "LY9WHT",
            GhnOrderCode = "LY9WHT",
            Status = ShipmentStatus.ReadyToPick,
            ReceiverName = "Tran Van An",
            ReceiverPhone = "0901112222",
            ReceiverAddress = "Quan 1, HCM",
            CreatedAt = t0,
            UpdatedAt = t0.AddMinutes(30)
        };

        var s2 = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId2,
            WarehouseId = wh.Id,
            CarrierId = Guid.NewGuid(),
            TrackingNumber = "GHN888222",
            GhnOrderCode = "GHN888222",
            Status = ShipmentStatus.InTransit,
            ReceiverName = "Nguyen Thi Binh",
            ReceiverPhone = "0903334444",
            ReceiverAddress = "Quan 3, HCM",
            CreatedAt = t0.AddHours(1),
            UpdatedAt = t0.AddHours(4)
        };

        var s3 = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId3,
            WarehouseId = wh.Id,
            CarrierId = Guid.NewGuid(),
            TrackingNumber = "GHN999333",
            GhnOrderCode = "GHN999333",
            Status = ShipmentStatus.Delivered,
            ReceiverName = "Le Minh Chau",
            ReceiverPhone = "0905556666",
            ReceiverAddress = "Ha Noi",
            CreatedAt = t0.AddHours(2),
            UpdatedAt = t0.AddHours(2)
        };

        db.Shipments.AddRange(s1, s2, s3);
        await db.SaveChangesAsync();

        var service = new ShipmentService(db, new MockCarrierResolver(new MockCarrierGateway()));

        // Search by lowercase partial tracking number
        var byTracking = await service.ListShipmentsAsync(new ShipmentFilterRequest(Search: "ly9w"), Guid.NewGuid(), isAdmin: true);
        byTracking.Items.Should().ContainSingle(x => x.TrackingNumber == "LY9WHT");
        byTracking.Items[0].CreatedAt.Should().Be(s1.CreatedAt);
        byTracking.Items[0].UpdatedAt.Should().Be(s1.UpdatedAt);

        // Search by SubOrder GUID
        var byOrderGuid = await service.ListShipmentsAsync(new ShipmentFilterRequest(Search: orderId2.ToString()), Guid.NewGuid(), isAdmin: true);
        byOrderGuid.Items.Should().ContainSingle(x => x.OrderId == orderId2);

        // Search by receiver phone
        var byPhone = await service.ListShipmentsAsync(new ShipmentFilterRequest(Search: "0905556666"), Guid.NewGuid(), isAdmin: true);
        byPhone.Items.Should().ContainSingle(x => x.TrackingNumber == "GHN999333");

        // Multi-status filter (comma-separated)
        var multiStatus = await service.ListShipmentsAsync(new ShipmentFilterRequest(Status: "ReadyToPick,Delivered"), Guid.NewGuid(), isAdmin: true);
        multiStatus.Items.Should().HaveCount(2);
        multiStatus.Items.Select(x => x.TrackingNumber).Should().BeEquivalentTo(new[] { "LY9WHT", "GHN999333" });

        // Sort by createdAt asc
        var createdAsc = await service.ListShipmentsAsync(new ShipmentFilterRequest(SortBy: "createdAt", SortOrder: "asc"), Guid.NewGuid(), isAdmin: true);
        createdAsc.Items.Select(x => x.TrackingNumber).Should().ContainInOrder("LY9WHT", "GHN888222", "GHN999333");

        // Sort by updatedAt desc (s2 was updated last)
        var updatedDesc = await service.ListShipmentsAsync(new ShipmentFilterRequest(SortBy: "updatedAt", SortOrder: "desc"), Guid.NewGuid(), isAdmin: true);
        updatedDesc.Items.Select(x => x.TrackingNumber).Should().ContainInOrder("GHN888222", "GHN999333", "LY9WHT");

        // Numeric status must be rejected
        var invalidNumeric = () => service.ListShipmentsAsync(new ShipmentFilterRequest(Status: "0"), Guid.NewGuid(), isAdmin: true);
        await invalidNumeric.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task GetByTrackingNumberAsync_ShouldMatchCaseInsensitiveTracking_AndSubOrderGuid()
    {
        using var db = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var wh = new Warehouse { Id = Guid.NewGuid(), SellerId = sellerId, Code = "WH1", Name = "Warehouse 1", Address = "HCM" };
        db.Warehouses.Add(wh);

        var orderId = Guid.NewGuid();
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            WarehouseId = wh.Id,
            CarrierId = Guid.NewGuid(),
            TrackingNumber = "LY9WHT",
            GhnOrderCode = "LY9WHT",
            ClientOrderCode = orderId.ToString(),
            Status = ShipmentStatus.ReadyToPick,
            ReceiverName = "Buyer",
            ReceiverPhone = "0901234567",
            ReceiverAddress = "HCM"
        };
        db.Shipments.Add(shipment);
        await db.SaveChangesAsync();

        var service = new ShipmentService(db, new MockCarrierResolver(new MockCarrierGateway()));

        var byLowerTracking = await service.GetByTrackingNumberAsync("ly9wht", Guid.NewGuid(), isAdminOrStaff: true);
        byLowerTracking.Id.Should().Be(shipment.Id);

        var byOrderId = await service.GetByTrackingNumberAsync(orderId.ToString(), Guid.NewGuid(), isAdminOrStaff: true);
        byOrderId.Id.Should().Be(shipment.Id);
    }
}
