namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ShipmentServiceTests
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

        public Task<CarrierFeeCalculationResult> CalculateFeeAsync(CarrierFeeCalculationRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CarrierFeeCalculationResult(
                TotalFee: 28000m,
                ExpectedDeliveryTime: DateTimeOffset.UtcNow.AddDays(2),
                ServiceCode: "GHN_STANDARD"));
        }

        public Task<CarrierCreateShipmentResult> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CarrierCreateShipmentResult(
                TrackingCode: "GHN_TEST_123456",
                ShippingFee: 28000m,
                ExpectedDeliveryTime: DateTimeOffset.UtcNow.AddDays(2),
                InitialStatus: ShipmentStatus.ReadyToPick,
                LabelUrl: "https://labels.ghn.vn/GHN_TEST_123456.pdf"));
        }

        public Task<CarrierStatusResult> GetStatusAsync(string trackingCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CarrierStatusResult(
                TrackingCode: trackingCode,
                Status: ShipmentStatus.InTransit,
                RawStatus: "delivering",
                Description: "In delivery",
                UpdatedAt: DateTimeOffset.UtcNow));
        }

        public Task<CarrierCancelResult> CancelAsync(string trackingCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CarrierCancelResult(trackingCode, true, "Success"));
        }

        public ShipmentStatus MapStatus(string rawCarrierStatus) => ShipmentStatus.ReadyToPick;
    }

    private sealed class FakeCarrierResolver : ICarrierResolver
    {
        private readonly ICarrierGateway _carrier;
        public FakeCarrierResolver(ICarrierGateway carrier) => _carrier = carrier;
        public ICarrierGateway Get(string? carrierCode = null) => _carrier;
    }

    [Fact]
    public async Task CalculateShippingFeeAsync_ShouldDynamicallyResolveSellerWarehouse_AndCalculateFee()
    {
        // Arrange
        using var db = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            Code = "WH_SELLER_01",
            Name = "Seller Dedicated Warehouse",
            Address = "123 Nguyen Hue, Q1, HCM",
            IsActive = true
        };
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();

        var fakeCarrier = new FakeCarrierGateway();
        var resolver = new FakeCarrierResolver(fakeCarrier);
        var service = new ShipmentService(db, resolver);

        var request = new ShopShippingQuoteRequest(
            SellerId: sellerId,
            FromDistrictId: 1442,
            ToDistrictId: 1450,
            ToWardCode: "20804",
            WeightInGrams: 500,
            Items: new List<ShippingQuoteItem>
            {
                new(Guid.NewGuid(), 1, 500)
            });

        // Act
        var result = await service.CalculateShippingFeeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Fee.Should().Be(28000m);
        result.IsEstimatedFallback.Should().BeFalse();
        result.ServiceName.Should().Be("GHN_STANDARD");
    }

    [Fact]
    public async Task CreateShipmentAsync_ShouldDynamicallyResolveWarehouseAndCarrier_AndSaveShipment()
    {
        // Arrange
        using var db = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            Code = "WH_SELLER_99",
            Name = "Shop 99 Warehouse",
            Address = "District 1, HCM",
            IsActive = true
        };
        var carrier = new Carrier
        {
            Id = Guid.NewGuid(),
            Code = "GHN",
            Name = "Giao Hang Nhanh",
            IsActive = true
        };
        db.Warehouses.Add(warehouse);
        db.Carriers.Add(carrier);
        await db.SaveChangesAsync();

        var fakeCarrier = new FakeCarrierGateway();
        var resolver = new FakeCarrierResolver(fakeCarrier);
        var service = new ShipmentService(db, resolver);

        var subOrderId = Guid.NewGuid();
        var request = new CreateShipmentRequest(
            OrderId: Guid.NewGuid(),
            SubOrderId: subOrderId,
            SellerId: sellerId,
            RecipientName: "Tran Van B",
            RecipientPhone: "0912345678",
            ShippingAddress: "456 Le Loi, Q1",
            ToDistrictId: 1442,
            ToWardCode: "20101",
            WeightInGrams: 500);

        // Act
        var response = await service.CreateShipmentAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.TrackingNumber.Should().Be("GHN_TEST_123456");
        response.Status.Should().Be(ShipmentStatus.ReadyToPick.ToString());
        response.LabelUrl.Should().Be("https://labels.ghn.vn/GHN_TEST_123456.pdf");

        // Verify database persistence
        var savedShipment = await db.Shipments.FirstOrDefaultAsync(s => s.OrderId == subOrderId);
        savedShipment.Should().NotBeNull();
        savedShipment!.WarehouseId.Should().Be(warehouse.Id);
        savedShipment.CarrierId.Should().Be(carrier.Id);
        savedShipment.TrackingNumber.Should().Be("GHN_TEST_123456");
    }

    [Fact]
    public async Task CreateShipmentAsync_WhenCalledAgainForSameSubOrder_ReturnsExistingShipmentIdempotently()
    {
        // Arrange
        using var db = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            Code = "WH_SELLER_02",
            Name = "Warehouse",
            IsActive = true
        };
        var carrier = new Carrier
        {
            Id = Guid.NewGuid(),
            Code = "GHN",
            Name = "GHN",
            IsActive = true
        };
        db.Warehouses.Add(warehouse);
        db.Carriers.Add(carrier);
        await db.SaveChangesAsync();

        var fakeCarrier = new FakeCarrierGateway();
        var resolver = new FakeCarrierResolver(fakeCarrier);
        var service = new ShipmentService(db, resolver);

        var subOrderId = Guid.NewGuid();
        var request = new CreateShipmentRequest(
            OrderId: Guid.NewGuid(),
            SubOrderId: subOrderId,
            SellerId: sellerId,
            RecipientName: "Tran Van B",
            RecipientPhone: "0912345678",
            ShippingAddress: "456 Le Loi, Q1",
            ToDistrictId: 1442,
            ToWardCode: "20101",
            WeightInGrams: 500);

        // First call
        var firstResponse = await service.CreateShipmentAsync(request);

        // Second call with same subOrderId
        var secondResponse = await service.CreateShipmentAsync(request);

        // Assert
        secondResponse.Should().NotBeNull();
        secondResponse.ShipmentId.Should().Be(firstResponse.ShipmentId);
        secondResponse.TrackingNumber.Should().Be(firstResponse.TrackingNumber);

        // Only one shipment in database
        var count = await db.Shipments.CountAsync(s => s.OrderId == subOrderId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task CreateShipmentAsync_WhenNoWarehouseExists_ThrowsEntityNotFoundException()
    {
        // Arrange
        using var db = CreateDbContext();
        var fakeCarrier = new FakeCarrierGateway();
        var resolver = new FakeCarrierResolver(fakeCarrier);
        var service = new ShipmentService(db, resolver);

        var request = new CreateShipmentRequest(
            OrderId: Guid.NewGuid(),
            SubOrderId: Guid.NewGuid(),
            SellerId: Guid.NewGuid(),
            RecipientName: "Tran Van B",
            RecipientPhone: "0912345678",
            ShippingAddress: "456 Le Loi, Q1",
            ToDistrictId: 1442,
            ToWardCode: "20101",
            WeightInGrams: 500);

        // Act & Assert
        var act = async () => await service.CreateShipmentAsync(request);
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}

