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

/// <summary>
/// A shop ships from its own warehouse. A seller with no active warehouse of their own must get WAREHOUSE_NOT_FOUND from the shipping fee
/// and from shipment creation, and must never be given another seller's warehouse (wrong pickup address, wrong fee).
/// </summary>
public sealed class SellerWarehouseTests
{
    private sealed class RecordingGateway : ICarrierGateway
    {
        public string Code => "GHN";
        public int Created { get; private set; }
        public int FeeCalls { get; private set; }
        public CarrierCreateShipmentRequest? LastCreate { get; private set; }

        public Task<CarrierFeeCalculationResult> CalculateFeeAsync(CarrierFeeCalculationRequest request, CancellationToken cancellationToken = default)
        {
            FeeCalls++;
            return Task.FromResult(new CarrierFeeCalculationResult(25000m));
        }

        public Task<CarrierCreateShipmentResult> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default)
        {
            Created++;
            LastCreate = request;
            return Task.FromResult(new CarrierCreateShipmentResult("GHN_SW_001", 25000m, DateTimeOffset.UtcNow.AddDays(2), ShipmentStatus.ReadyToPick));
        }

        public Task<CarrierStatusResult> GetStatusAsync(string trackingCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CarrierStatusResult(trackingCode, ShipmentStatus.ReadyToPick, "ready_to_pick"));

        public Task<CarrierCancelResult> CancelAsync(string trackingCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CarrierCancelResult(trackingCode, true, "ok"));

        public Task<string?> GetPrintTokenAsync(string trackingCode, CancellationToken cancellationToken = default) => Task.FromResult<string?>("t");

        public ShipmentStatus MapStatus(string rawCarrierStatus) => ShipmentStatus.ReadyToPick;
    }

    private sealed class Resolver(ICarrierGateway gateway) : ICarrierResolver
    {
        public ICarrierGateway Get(string? carrierCode = null) => gateway;
    }

    private static async Task<(AppDbContext Db, ShipmentService Service, RecordingGateway Gateway)> SetUpAsync(params Warehouse[] warehouses)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Code = "GHN", Name = "GHN", Priority = 1, IsActive = true });
        db.Warehouses.AddRange(warehouses);
        await db.SaveChangesAsync();
        var gateway = new RecordingGateway();
        return (db, new ShipmentService(db, new Resolver(gateway)), gateway);
    }

    private static Warehouse Warehouse(Guid? sellerId, string name, int district = 1442, bool active = true) => new()
    {
        Id = Guid.NewGuid(), SellerId = sellerId, Code = "W-" + Guid.NewGuid().ToString("N")[..6], Name = name, Address = $"{name} street",
        ContactPhone = "0912345678", ProvinceId = district > 0 ? 202 : null, DistrictId = district, WardCode = "20101", IsActive = active
    };

    private static CreateShipmentRequest Create(Guid sellerId) => new(
        OrderId: Guid.NewGuid(), SubOrderId: Guid.NewGuid(), SellerId: sellerId, RecipientName: "Buyer", RecipientPhone: "0900000001",
        ShippingAddress: "12 Nguyen Hue", ToDistrictId: 1442, ToWardCode: "20101");

    private static ShopShippingQuoteRequest Quote(Guid sellerId, int fromDistrict = 0) => new(
        SellerId: sellerId, FromDistrictId: fromDistrict, ToDistrictId: 1442, ToWardCode: "20101", WeightInGrams: 500);

    [Fact]
    public async Task ReadingTheOwnWarehouse_NeverCreatesOne()
    {
        var withWarehouse = Guid.NewGuid();
        var without = Guid.NewGuid();
        var (db, _, _) = await SetUpAsync(Warehouse(withWarehouse, "My shop"));
        using var _ = db;
        var warehouses = new Fulfillment.Application.Services.Warehouse.WarehouseService(db, new AcceptingLocations());

        (await warehouses.GetForSellerAsync(withWarehouse))!.Name.Should().Be("My shop");
        (await warehouses.GetForSellerAsync(without, shopName: "New shop")).Should().BeNull();

        (await db.Warehouses.CountAsync()).Should().Be(1);   // asking did not create a placeholder
    }

    [Fact]
    public async Task CreateShipment_UsesTheSellersOwnWarehouse()
    {
        var seller = Guid.NewGuid();
        var other = Guid.NewGuid();
        var (db, service, gateway) = await SetUpAsync(Warehouse(other, "Other shop"), Warehouse(seller, "My shop"));
        using var _ = db;

        await service.CreateShipmentAsync(Create(seller));

        gateway.Created.Should().Be(1);
        var shipment = await db.Shipments.Include(s => s.Warehouse).SingleAsync();
        shipment.Warehouse!.SellerId.Should().Be(seller);
    }

    [Fact]
    public async Task CreateShipment_ForASellerWithoutAWarehouse_IsRefused_EvenWhenAnotherSellerHasOne()
    {
        var (db, service, gateway) = await SetUpAsync(Warehouse(Guid.NewGuid(), "Other shop"));
        using var _ = db;

        var act = () => service.CreateShipmentAsync(Create(Guid.NewGuid()));

        (await act.Should().ThrowAsync<EntityNotFoundException>()).Which.ErrorCode.Should().Be("WAREHOUSE_NOT_FOUND");
        gateway.Created.Should().Be(0);
        (await db.Shipments.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateShipment_ForASellerWhoseOnlyWarehouseIsInactive_IsRefused()
    {
        var seller = Guid.NewGuid();
        var (db, service, gateway) = await SetUpAsync(Warehouse(seller, "Closed shop", active: false), Warehouse(Guid.NewGuid(), "Other shop"));
        using var _ = db;

        var act = () => service.CreateShipmentAsync(Create(seller));

        (await act.Should().ThrowAsync<EntityNotFoundException>()).Which.ErrorCode.Should().Be("WAREHOUSE_NOT_FOUND");
        gateway.Created.Should().Be(0);
    }

    [Fact]
    public async Task CreateShipment_ForAWarehouseWithNoOwner_IsNotUsedByAnotherSeller()
    {
        var (db, service, gateway) = await SetUpAsync(Warehouse(null, "Shared leftover"));
        using var _ = db;

        var act = () => service.CreateShipmentAsync(Create(Guid.NewGuid()));

        (await act.Should().ThrowAsync<EntityNotFoundException>()).Which.ErrorCode.Should().Be("WAREHOUSE_NOT_FOUND");
        gateway.Created.Should().Be(0);
    }

    [Fact]
    public async Task ShippingFee_ForASellerWithoutAWarehouse_IsRefused_AndTheCarrierIsNotAsked()
    {
        var (db, service, gateway) = await SetUpAsync(Warehouse(Guid.NewGuid(), "Other shop"));
        using var _ = db;

        var act = () => service.CalculateShippingFeeAsync(Quote(Guid.NewGuid()));

        (await act.Should().ThrowAsync<EntityNotFoundException>()).Which.ErrorCode.Should().Be("WAREHOUSE_NOT_FOUND");
        gateway.FeeCalls.Should().Be(0);
    }

    [Fact]
    public async Task ShippingFee_UsesTheSellersWarehouse()
    {
        var seller = Guid.NewGuid();
        var (db, service, gateway) = await SetUpAsync(Warehouse(Guid.NewGuid(), "Other shop", district: 999), Warehouse(seller, "My shop", district: 1442));
        using var _ = db;

        var quote = await service.CalculateShippingFeeAsync(Quote(seller));

        quote.Fee.Should().BeGreaterThan(0);
        gateway.FeeCalls.Should().Be(1);
    }

    [Fact]
    public async Task ShippingFee_WhenTheCallerNamesTheOriginDistrict_DoesNotNeedAWarehouse()
    {
        var (db, service, gateway) = await SetUpAsync();
        using var _ = db;

        var quote = await service.CalculateShippingFeeAsync(Quote(Guid.NewGuid(), fromDistrict: 1442));

        quote.Fee.Should().BeGreaterThan(0);
        gateway.FeeCalls.Should().Be(1);
    }
}
