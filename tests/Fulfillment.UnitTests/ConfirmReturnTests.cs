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
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ConfirmReturnTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class DummyCarrierResolver : ICarrierResolver
    {
        public ICarrierGateway Get(string? carrierCode = null) => throw new NotImplementedException();
        public ICarrierGateway Resolve(ShippingServiceType serviceType) => throw new NotImplementedException();
    }

    private async Task<(AppDbContext Context, ShipmentService Service, Guid SellerId, Guid OrderId, Guid SkuId, Guid WarehouseId)> SetupShipmentAsync(
        ShipmentStatus status,
        bool withInventory = true)
    {
        var context = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var carrierId = Guid.NewGuid();

        var carrier = new Carrier
        {
            Id = carrierId,
            Code = "GHN",
            Name = "Giao Hang Nhanh",
            IsActive = true
        };
        context.Carriers.Add(carrier);

        var warehouse = new Warehouse
        {
            Id = warehouseId,
            Code = "WH-TEST",
            SellerId = sellerId,
            Name = "Seller Warehouse",
            Address = "123 Street",
            ContactPhone = "0901234567",
            DistrictId = 1442,
            WardCode = "20101",
            IsActive = true
        };
        context.Warehouses.Add(warehouse);

        if (withInventory)
        {
            var invItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                SkuId = skuId,
                WarehouseId = warehouseId,
                AvailableQuantity = 10,
                ReservedQuantity = 0,
                TotalQuantity = 10
            };
            context.InventoryItems.Add(invItem);
        }

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            WarehouseId = warehouseId,
            CarrierId = carrierId,
            Status = status,
            ReceiverName = "Buyer",
            ReceiverPhone = "0909999999",
            ReceiverAddress = "456 Avenue",
            CreatedBy = sellerId,
            Warehouse = warehouse,
            Items = withInventory
                ? new List<ShipmentItem>
                {
                    new()
                    {
                        ShipmentId = shipmentId,
                        SkuId = skuId,
                        Quantity = 2
                    }
                }
                : new List<ShipmentItem>()
        };
        context.Shipments.Add(shipment);
        await context.SaveChangesAsync();

        var service = new ShipmentService(context, new DummyCarrierResolver());
        return (context, service, sellerId, orderId, skuId, warehouseId);
    }

    [Fact]
    public async Task CarrierAlreadyReportedReturned_SellerConfirmationStillRestoresTheStock()
    {
        var (context, service, sellerId, orderId, skuId, warehouseId) = await SetupShipmentAsync(ShipmentStatus.Returned);

        var result = await service.ConfirmReturnAsync(orderId, sellerId);

        result.Success.Should().BeTrue();
        result.Code.Should().Be("STOCK_RESTORED");
        result.Data.RestoredStock.Should().Be(2);

        var inv = await context.InventoryItems.FirstAsync(i => i.SkuId == skuId && i.WarehouseId == warehouseId);
        inv.AvailableQuantity.Should().Be(12);

        var ledgers = await context.InventoryLedgers.Where(l => l.ReferenceType == "ORDER_RETURN").ToListAsync();
        ledgers.Should().HaveCount(1);
        ledgers[0].Quantity.Should().Be(2);
    }

    [Fact]
    public async Task ConfirmingTwice_RestoresTheStockOnce()
    {
        var (context, service, sellerId, orderId, skuId, warehouseId) = await SetupShipmentAsync(ShipmentStatus.DeliveryFailed);

        var first = await service.ConfirmReturnAsync(orderId, sellerId);
        first.Code.Should().Be("STOCK_RESTORED");
        first.Data.RestoredStock.Should().Be(2);

        var second = await service.ConfirmReturnAsync(orderId, sellerId);
        second.Success.Should().BeTrue();
        second.Code.Should().Be("ALREADY_RETURNED");
        second.Data.RestoredStock.Should().Be(0);

        var inv = await context.InventoryItems.FirstAsync(i => i.SkuId == skuId && i.WarehouseId == warehouseId);
        inv.AvailableQuantity.Should().Be(12);

        var ledgers = await context.InventoryLedgers.Where(l => l.ReferenceType == "ORDER_RETURN").ToListAsync();
        ledgers.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(ShipmentStatus.ReadyToPick)]
    [InlineData(ShipmentStatus.Picking)]
    [InlineData(ShipmentStatus.HandedOver)]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Cancelled)]
    public async Task ParcelThatIsNotComingBack_CannotBeConfirmed_AndStockIsUntouched(ShipmentStatus status)
    {
        var (context, service, sellerId, orderId, skuId, warehouseId) = await SetupShipmentAsync(status);

        var act = () => service.ConfirmReturnAsync(orderId, sellerId);

        var ex = await act.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.ErrorCode.Should().Be("SHIPMENT_NOT_RETURNING");
        ex.Which.StatusCode.Should().Be(409);

        var inv = await context.InventoryItems.FirstAsync(i => i.SkuId == skuId && i.WarehouseId == warehouseId);
        inv.AvailableQuantity.Should().Be(10);
    }

    [Fact]
    public async Task FailedDelivery_RestoresTheStock_AndLeavesTheShipmentStatusToTheCarrier()
    {
        var (context, service, sellerId, orderId, skuId, warehouseId) = await SetupShipmentAsync(ShipmentStatus.DeliveryFailed);

        var result = await service.ConfirmReturnAsync(orderId, sellerId);

        result.Success.Should().BeTrue();
        result.Code.Should().Be("STOCK_RESTORED");

        var shipment = await context.Shipments.FirstAsync(s => s.OrderId == orderId);
        shipment.Status.Should().Be(ShipmentStatus.DeliveryFailed); // Remains for carrier to update

        var inv = await context.InventoryItems.FirstAsync(i => i.SkuId == skuId && i.WarehouseId == warehouseId);
        inv.AvailableQuantity.Should().Be(12);
    }

    [Fact]
    public async Task SomeoneElsesShipment_IsForbidden()
    {
        var (context, service, _, orderId, _, _) = await SetupShipmentAsync(ShipmentStatus.DeliveryFailed);
        var intruderId = Guid.NewGuid();

        var act = () => service.ConfirmReturnAsync(orderId, intruderId);

        var ex = await act.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.ErrorCode.Should().Be("FORBIDDEN");
        ex.Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task AnAdmin_CanConfirmAnyReturnedShipment()
    {
        var (context, service, _, orderId, skuId, warehouseId) = await SetupShipmentAsync(ShipmentStatus.DeliveryFailed);
        var adminId = Guid.NewGuid();

        var result = await service.ConfirmReturnAsync(orderId, adminId, isAdmin: true);

        result.Success.Should().BeTrue();
        result.Code.Should().Be("STOCK_RESTORED");

        var inv = await context.InventoryItems.FirstAsync(i => i.SkuId == skuId && i.WarehouseId == warehouseId);
        inv.AvailableQuantity.Should().Be(12);
    }

    [Fact]
    public async Task ShipmentWithoutInventoryRows_ConfirmsWithZeroRestored_ForExampleAuctionItems()
    {
        var (context, service, sellerId, orderId, _, _) = await SetupShipmentAsync(ShipmentStatus.DeliveryFailed, withInventory: false);

        var result = await service.ConfirmReturnAsync(orderId, sellerId);

        result.Success.Should().BeTrue();
        result.Code.Should().Be("STOCK_RESTORED");
        result.Data.RestoredStock.Should().Be(0);
    }
}
