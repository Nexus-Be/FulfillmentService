namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Fulfillment.Application.Exceptions;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Inventory;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;
using Fulfillment.Infrastructure.Persistence;
using Fulfillment.Infrastructure.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public class ShipmentSecurityAndLocationTests
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
        public bool UpdateOrderCalled { get; private set; }

        public Task<CarrierFeeCalculationResult> CalculateFeeAsync(CarrierFeeCalculationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new CarrierFeeCalculationResult(25000m));

        public Task<CarrierCreateShipmentResult> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new CarrierCreateShipmentResult("GHN_TEST_001", 25000m, DateTimeOffset.UtcNow.AddDays(2), ShipmentStatus.ReadyToPick));

        public Task<CarrierStatusResult> GetStatusAsync(string trackingCode, CancellationToken cancellationToken = default)
            => Task.FromResult(new CarrierStatusResult(trackingCode, ShipmentStatus.InTransit, "in_transit", "Parcel in transit", DateTimeOffset.UtcNow));

        public Task<CarrierCancelResult> CancelAsync(string trackingCode, CancellationToken cancellationToken = default)
        {
            CancelCalled = true;
            return Task.FromResult(new CarrierCancelResult(trackingCode, true, "Cancelled by test"));
        }

        public Task<string?> GetPrintTokenAsync(string trackingCode, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("PRINT_TOKEN_TEST");

        public Task<bool> UpdateOrderAsync(string trackingCode, string toName, string toPhone, string toAddress, string? toWardCode, int? toDistrictId, CancellationToken cancellationToken = default)
        {
            UpdateOrderCalled = true;
            return Task.FromResult(true);
        }

        public ShipmentStatus MapStatus(string rawCarrierStatus) => ShipmentStatus.InTransit;
    }

    private sealed class MockCarrierResolver : ICarrierResolver
    {
        private readonly ICarrierGateway _gateway;
        public MockCarrierResolver(ICarrierGateway gateway) => _gateway = gateway;
        public ICarrierGateway Get(string? carrierCode = null) => _gateway;
        public ICarrierGateway Resolve(ShippingServiceType serviceType) => _gateway;
    }

    [Fact]
    public async Task GetLedgersBySkuId_WhenNotOwnerAndNotAdmin_Throws403Forbidden()
    {
        using var context = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var skuId = Guid.NewGuid();

        var warehouse = new Warehouse
        {
            Id = warehouseId,
            Code = "WH-IDOR-1",
            Name = "Seller Warehouse",
            SellerId = sellerId,
            Address = "123 St",
            ContactPhone = "0900000000",
            DistrictId = 1442,
            WardCode = "20101",
            IsActive = true
        };
        context.Warehouses.Add(warehouse);

        var invItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            SkuId = skuId,
            WarehouseId = warehouseId,
            Warehouse = warehouse,
            AvailableQuantity = 10,
            ReservedQuantity = 0,
            TotalQuantity = 10
        };
        context.InventoryItems.Add(invItem);
        await context.SaveChangesAsync();

        var service = new InventoryAppService(context);

        var act = () => service.GetLedgersBySkuIdAsync(skuId, currentUserId: strangerId, isAdmin: false);

        var ex = await act.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.ErrorCode.Should().Be("FORBIDDEN");
        ex.Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetShipmentByTrackingNumber_WhenStranger_Throws403Forbidden()
    {
        using var context = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var carrierId = Guid.NewGuid();

        var warehouse = new Warehouse
        {
            Id = warehouseId,
            Code = "WH-TRK-1",
            Name = "Seller WH",
            SellerId = sellerId,
            Address = "123 St",
            ContactPhone = "0900000000",
            DistrictId = 1442,
            WardCode = "20101",
            IsActive = true
        };
        context.Warehouses.Add(warehouse);

        var carrier = new Carrier { Id = carrierId, Code = "GHN", Name = "GHN", IsActive = true };
        context.Carriers.Add(carrier);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            WarehouseId = warehouseId,
            CarrierId = carrierId,
            TrackingNumber = "GHN_TRACKING_123",
            ReceiverName = "Secret Buyer",
            ReceiverPhone = "0912345678",
            ReceiverAddress = "789 Private Road",
            CreatedBy = sellerId,
            Warehouse = warehouse
        };
        context.Shipments.Add(shipment);
        await context.SaveChangesAsync();

        var service = new ShipmentService(context, new MockCarrierResolver(new MockCarrierGateway()));

        var act = () => service.GetByTrackingNumberAsync("GHN_TRACKING_123", currentUserId: strangerId, isAdminOrStaff: false);

        var ex = await act.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.ErrorCode.Should().Be("FORBIDDEN");
        ex.Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetShipmentByTrackingNumber_WhenBuyerMatches_Succeeds()
    {
        using var context = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var carrierId = Guid.NewGuid();

        var warehouse = new Warehouse
        {
            Id = warehouseId,
            Code = "WH-BUYER-1",
            Name = "Seller WH",
            SellerId = sellerId,
            Address = "123 St",
            ContactPhone = "0900000000",
            DistrictId = 1442,
            WardCode = "20101",
            IsActive = true
        };
        context.Warehouses.Add(warehouse);

        var carrier = new Carrier { Id = carrierId, Code = "GHN", Name = "GHN", IsActive = true };
        context.Carriers.Add(carrier);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            BuyerId = buyerId,
            WarehouseId = warehouseId,
            CarrierId = carrierId,
            TrackingNumber = "GHN_BUYER_123",
            ReceiverName = "Valid Buyer",
            ReceiverPhone = "0912345678",
            ReceiverAddress = "789 Avenue",
            CreatedBy = sellerId,
            Warehouse = warehouse
        };
        context.Shipments.Add(shipment);
        await context.SaveChangesAsync();

        var service = new ShipmentService(context, new MockCarrierResolver(new MockCarrierGateway()));

        var result = await service.GetByTrackingNumberAsync("GHN_BUYER_123", currentUserId: buyerId, isAdminOrStaff: false);

        result.Should().NotBeNull();
        result.BuyerId.Should().Be(buyerId);
        result.ReceiverName.Should().Be("Valid Buyer");
    }

    [Fact]
    public async Task CancelShipmentByOrderId_InternalS2S_CancelsCarrierAndUpdatesStatus()
    {
        using var context = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var carrierId = Guid.NewGuid();

        var warehouse = new Warehouse
        {
            Id = warehouseId,
            Code = "WH-CANCEL-1",
            Name = "Seller WH",
            SellerId = sellerId,
            Address = "123 St",
            ContactPhone = "0900000000",
            DistrictId = 1442,
            WardCode = "20101",
            IsActive = true
        };
        context.Warehouses.Add(warehouse);

        var carrier = new Carrier { Id = carrierId, Code = "GHN", Name = "GHN", IsActive = true };
        context.Carriers.Add(carrier);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            WarehouseId = warehouseId,
            CarrierId = carrierId,
            TrackingNumber = "GHN_CANCEL_123",
            Status = ShipmentStatus.ReadyToPick,
            ReceiverName = "Customer",
            ReceiverPhone = "0912345678",
            ReceiverAddress = "Address",
            CreatedBy = sellerId,
            Warehouse = warehouse,
            Carrier = carrier
        };
        context.Shipments.Add(shipment);
        await context.SaveChangesAsync();

        var mockGateway = new MockCarrierGateway();
        var service = new ShipmentService(context, new MockCarrierResolver(mockGateway));

        var response = await service.CancelShipmentByOrderIdAsync(orderId, "Customer cancelled order");

        response.IsSuccess.Should().BeTrue();
        mockGateway.CancelCalled.Should().BeTrue();

        var updatedShipment = await context.Shipments.Include(s => s.Events).FirstAsync(s => s.OrderId == orderId);
        updatedShipment.Status.Should().Be(ShipmentStatus.Cancelled);
        updatedShipment.Events.Should().Contain(e => e.Status == ShipmentEventStatus.Cancelled);
    }

    [Fact]
    public async Task UpdateRecipientAddress_WhenReadyToPick_UpdatesAddressAndRecordsEvent()
    {
        using var context = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var carrierId = Guid.NewGuid();

        var warehouse = new Warehouse
        {
            Id = warehouseId,
            Code = "WH-UPDATE-1",
            Name = "Seller WH",
            SellerId = sellerId,
            Address = "123 St",
            ContactPhone = "0900000000",
            DistrictId = 1442,
            WardCode = "20101",
            IsActive = true
        };
        context.Warehouses.Add(warehouse);

        var carrier = new Carrier { Id = carrierId, Code = "GHN", Name = "GHN", IsActive = true };
        context.Carriers.Add(carrier);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            WarehouseId = warehouseId,
            CarrierId = carrierId,
            TrackingNumber = "GHN_ADDR_123",
            Status = ShipmentStatus.ReadyToPick,
            ReceiverName = "Old Name",
            ReceiverPhone = "0911111111",
            ReceiverAddress = "Old Address",
            CreatedBy = sellerId,
            Warehouse = warehouse,
            Carrier = carrier
        };
        context.Shipments.Add(shipment);
        await context.SaveChangesAsync();

        var mockGateway = new MockCarrierGateway();
        var service = new ShipmentService(context, new MockCarrierResolver(mockGateway));

        var updateReq = new UpdateRecipientAddressRequest(
            RecipientName: "New Name",
            RecipientPhone: "0922222222",
            ShippingAddress: "New Address 123",
            ToProvinceName: "Hà Nội",
            ToDistrictId: 1442,
            ToWardCode: "20101");

        var updated = await service.UpdateRecipientAddressAsync(orderId, updateReq);

        updated.ReceiverName.Should().Be("New Name");
        updated.ReceiverPhone.Should().Be("0922222222");
        updated.ReceiverAddress.Should().Be("New Address 123");
        mockGateway.UpdateOrderCalled.Should().BeTrue();

        var shipmentInDb = await context.Shipments.Include(s => s.Events).FirstAsync(s => s.OrderId == orderId);
        shipmentInDb.ReceiverName.Should().Be("New Name");
        shipmentInDb.Events.Should().Contain(e => e.CarrierStatus == "address_updated");
    }

    [Fact]
    public async Task UpdateRecipientAddress_WhenAlreadyInTransit_Throws409Conflict()
    {
        using var context = CreateDbContext();
        var orderId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var carrierId = Guid.NewGuid();

        var warehouse = new Warehouse
        {
            Id = warehouseId,
            Code = "WH-INTRANSIT",
            Name = "WH",
            SellerId = Guid.NewGuid(),
            Address = "123 St",
            ContactPhone = "0900000000",
            DistrictId = 1442,
            WardCode = "20101",
            IsActive = true
        };
        context.Warehouses.Add(warehouse);

        var carrier = new Carrier { Id = carrierId, Code = "GHN", Name = "GHN", IsActive = true };
        context.Carriers.Add(carrier);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            WarehouseId = warehouseId,
            CarrierId = carrierId,
            Status = ShipmentStatus.InTransit,
            ReceiverName = "Name",
            ReceiverPhone = "0900000000",
            ReceiverAddress = "Address",
            CreatedBy = Guid.NewGuid(),
            Warehouse = warehouse,
            Carrier = carrier
        };
        context.Shipments.Add(shipment);
        await context.SaveChangesAsync();

        var service = new ShipmentService(context, new MockCarrierResolver(new MockCarrierGateway()));

        var updateReq = new UpdateRecipientAddressRequest("New Name", "0922222222", "New Address");

        var act = () => service.UpdateRecipientAddressAsync(orderId, updateReq);

        var ex = await act.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.ErrorCode.Should().Be("SHIPMENT_ALREADY_PICKED_UP");
        ex.Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task ValidateShippingLocation_ValidHierarchy_ReturnsValid()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new Fulfillment.Infrastructure.Carriers.Ghn.GhnOptions());
        var service = new GhnLocationService(options, cache, NullLogger<GhnLocationService>.Instance);

        var req = new ValidateShippingLocationRequest(
            ProvinceId: 201, // Hà Nội
            DistrictId: 1442, // Ba Đình
            WardCode: "20101"); // Phúc Xá

        var res = await service.ValidateLocationAsync(req);

        res.IsValid.Should().BeTrue();
        res.ProvinceName.Should().Be("Hà Nội");
        res.DistrictName.Should().Be("Quận Ba Đình");
        res.WardName.Should().Be("Phường Phúc Xá");
    }

    [Fact]
    public async Task ValidateShippingLocation_InvalidDistrictForProvince_ReturnsInvalid()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new Fulfillment.Infrastructure.Carriers.Ghn.GhnOptions());
        var service = new GhnLocationService(options, cache, NullLogger<GhnLocationService>.Instance);

        var req = new ValidateShippingLocationRequest(
            ProvinceId: 201, // Hà Nội
            DistrictId: 1446, // Quận 1 (belongs to TP.HCM, not Hà Nội)
            WardCode: "20101");

        var res = await service.ValidateLocationAsync(req);

        res.IsValid.Should().BeFalse();
        res.Message.Should().Contain("does not belong to province");
    }
}
