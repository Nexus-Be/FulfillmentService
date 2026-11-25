namespace Fulfillment.UnitTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Application.Exceptions;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Application.Services.Warehouse;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// A warehouse address is saved only as a whole province, district and ward chain that the GHN lists confirmed; a warehouse without any address
/// is a placeholder that cannot quote or ship. With no trusted GHN source nothing is saved.
/// </summary>
public sealed class WarehouseAddressVerificationTests
{
    private static AppDbContext NewDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static InternalCreateWarehouseRequest Internal(Guid seller, int? province = 202, int? district = 1442, string? ward = "20101") =>
        new(seller, seller, "Shop", "Owner", "0912345678", "1 Le Loi", province, district, ward);

    [Fact]
    public async Task A_confirmed_chain_is_saved_with_the_province_id_and_the_province_name_from_GHN()
    {
        using var db = NewDb();
        var locations = new AcceptingLocations();
        var service = new WarehouseService(db, locations);
        var seller = Guid.NewGuid();

        await service.CreateInternalAsync(Internal(seller));

        var saved = await db.Warehouses.SingleAsync();
        saved.ProvinceId.Should().Be(202);
        saved.DistrictId.Should().Be(1442);
        saved.WardCode.Should().Be("20101");
        saved.ProvinceName.Should().Be("Ho Chi Minh");
        locations.Asked.Should().ContainSingle().Which.Should().Be((202, 1442, "20101"));
    }

    [Fact]
    public async Task A_placeholder_without_any_address_is_saved_without_asking_GHN()
    {
        using var db = NewDb();
        var locations = new AcceptingLocations();
        var service = new WarehouseService(db, locations);

        await service.CreateInternalAsync(Internal(Guid.NewGuid(), province: null, district: null, ward: null));

        (await db.Warehouses.SingleAsync()).ProvinceId.Should().BeNull();
        locations.Checks.Should().Be(0);
    }

    [Theory]
    [InlineData(null, 1442, "20101")]
    [InlineData(202, null, "20101")]
    [InlineData(202, 1442, null)]
    [InlineData(202, 1442, "  ")]
    public async Task A_part_of_the_chain_is_refused_and_nothing_is_saved(int? province, int? district, string? ward)
    {
        using var db = NewDb();
        var service = new WarehouseService(db, new AcceptingLocations());

        var act = () => service.CreateInternalAsync(Internal(Guid.NewGuid(), province, district, ward));

        (await act.Should().ThrowAsync<BusinessRuleValidationException>()).Which.ErrorCode.Should().Be("WAREHOUSE_ADDRESS_INCOMPLETE");
        (await db.Warehouses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task A_chain_GHN_does_not_confirm_is_400_and_nothing_is_saved()
    {
        using var db = NewDb();
        var service = new WarehouseService(db, new AcceptingLocations { Reject = true });

        var act = () => service.CreateInternalAsync(Internal(Guid.NewGuid()));

        var thrown = (await act.Should().ThrowAsync<BusinessRuleValidationException>()).Which;
        thrown.ErrorCode.Should().Be("VAL_INVALID_LOCATION");
        thrown.StatusCode.Should().Be(400);
        (await db.Warehouses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task With_no_trusted_GHN_source_the_address_is_503_and_nothing_is_saved()
    {
        using var db = NewDb();
        var service = new WarehouseService(db, new AcceptingLocations { SourceDown = true });

        var act = () => service.CreateInternalAsync(Internal(Guid.NewGuid()));

        var thrown = (await act.Should().ThrowAsync<BusinessRuleValidationException>()).Which;
        thrown.ErrorCode.Should().Be("LOCATION_SOURCE_UNAVAILABLE");
        thrown.StatusCode.Should().Be(503);
        (await db.Warehouses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task The_seller_update_checks_a_changed_address_as_a_whole_chain_and_a_name_change_is_not_held_up()
    {
        using var db = NewDb();
        var locations = new AcceptingLocations();
        var service = new WarehouseService(db, locations);
        var seller = Guid.NewGuid();
        await service.CreateInternalAsync(Internal(seller));
        var id = (await db.Warehouses.SingleAsync()).Id;
        locations.Asked.Clear();

        // name only: the GHN lists are not asked, so an outage does not stop it
        locations.SourceDown = true;
        await service.UpdateAsync(id, new UpdateWarehouseRequest("New name"), seller);
        locations.Checks.Should().Be(1, "only the first save asked");   // Checks counts the create too
        (await db.Warehouses.SingleAsync()).Name.Should().Be("New name");

        // a ward change with the source down: 503, the old ward stays
        var act = () => service.UpdateAsync(id, new UpdateWarehouseRequest("New name", WardCode: "20102"), seller);
        (await act.Should().ThrowAsync<BusinessRuleValidationException>()).Which.StatusCode.Should().Be(503);
        (await db.Warehouses.SingleAsync()).WardCode.Should().Be("20101");

        // a ward change with the source up: the whole chain (the saved province and district plus the new ward) is what was checked
        locations.SourceDown = false;
        await service.UpdateAsync(id, new UpdateWarehouseRequest("New name", WardCode: "20102"), seller);
        locations.Asked[^1].Should().Be((202, 1442, "20102"));
        (await db.Warehouses.SingleAsync()).WardCode.Should().Be("20102");
    }

    [Fact]
    public async Task A_legacy_warehouse_without_a_province_id_cannot_change_only_its_ward()
    {
        using var db = NewDb();
        var seller = Guid.NewGuid();
        db.Warehouses.Add(new Warehouse { Id = Guid.NewGuid(), SellerId = seller, Code = "WH_LEGACY", Name = "Old", DistrictId = 1442, WardCode = "20101", IsActive = true });
        await db.SaveChangesAsync();
        var id = (await db.Warehouses.SingleAsync()).Id;
        var service = new WarehouseService(db, new AcceptingLocations());

        var act = () => service.UpdateAsync(id, new UpdateWarehouseRequest("Old", WardCode: "20102"), seller);

        (await act.Should().ThrowAsync<BusinessRuleValidationException>()).Which.ErrorCode.Should().Be("WAREHOUSE_ADDRESS_INCOMPLETE");
    }

    // ---- a warehouse that is not confirmed cannot quote or ship
    private sealed class Gateway : ICarrierGateway
    {
        public string Code => "GHN";
        public int Calls { get; private set; }
        public Task<CarrierFeeCalculationResult> CalculateFeeAsync(CarrierFeeCalculationRequest request, CancellationToken cancellationToken = default) { Calls++; return Task.FromResult(new CarrierFeeCalculationResult(25000m)); }
        public Task<CarrierCreateShipmentResult> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default) { Calls++; return Task.FromResult(new CarrierCreateShipmentResult("GHN_X", 25000m, DateTimeOffset.UtcNow.AddDays(2), ShipmentStatus.ReadyToPick)); }
        public Task<CarrierStatusResult> GetStatusAsync(string trackingCode, CancellationToken cancellationToken = default) => Task.FromResult(new CarrierStatusResult(trackingCode, ShipmentStatus.ReadyToPick, "ready_to_pick"));
        public Task<CarrierCancelResult> CancelAsync(string trackingCode, CancellationToken cancellationToken = default) => Task.FromResult(new CarrierCancelResult(trackingCode, true, "ok"));
        public Task<string?> GetPrintTokenAsync(string trackingCode, CancellationToken cancellationToken = default) => Task.FromResult<string?>("t");
        public ShipmentStatus MapStatus(string rawCarrierStatus) => ShipmentStatus.ReadyToPick;
    }

    private sealed class Resolver(ICarrierGateway gateway) : ICarrierResolver
    {
        public ICarrierGateway Get(string? carrierCode = null) => gateway;
    }

    private static async Task<(AppDbContext Db, ShipmentService Service, Gateway Gateway, Guid Seller)> ShipperAsync(int? provinceId, int? districtId, string? ward)
    {
        var db = NewDb();
        var seller = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Code = "GHN", Name = "GHN", Priority = 1, IsActive = true });
        db.Warehouses.Add(new Warehouse { Id = Guid.NewGuid(), SellerId = seller, Code = "WH_T", Name = "W", ContactPhone = "0912345678", ProvinceId = provinceId, DistrictId = districtId, WardCode = ward, IsActive = true });
        await db.SaveChangesAsync();
        var gateway = new Gateway();
        return (db, new ShipmentService(db, new Resolver(gateway)), gateway, seller);
    }

    [Theory]
    [InlineData(null, 1442, "20101")]
    [InlineData(202, 1442, null)]
    public async Task The_quote_from_a_warehouse_that_is_not_confirmed_is_refused_before_the_carrier_is_asked(int? province, int? district, string? ward)
    {
        var (db, service, gateway, seller) = await ShipperAsync(province, district, ward);
        using var _ = db;

        var act = () => service.CalculateShippingFeeAsync(new ShopShippingQuoteRequest(seller, 0, 1442, "20101", 500));

        (await act.Should().ThrowAsync<BusinessRuleValidationException>()).Which.ErrorCode.Should().Be("WAREHOUSE_ADDRESS_INCOMPLETE");
        gateway.Calls.Should().Be(0);
    }

    [Fact]
    public async Task A_quote_with_an_origin_district_given_by_the_caller_is_an_estimate_and_needs_no_confirmed_warehouse()
    {
        var (db, service, gateway, seller) = await ShipperAsync(null, 1442, "20101");
        using var _ = db;

        var quote = await service.CalculateShippingFeeAsync(new ShopShippingQuoteRequest(seller, 1442, 1442, "20101", 500));

        quote.Fee.Should().BeGreaterThan(0);
        gateway.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Creating_a_shipment_from_a_warehouse_that_is_not_confirmed_is_refused_before_the_carrier_is_called()
    {
        var (db, service, gateway, seller) = await ShipperAsync(null, 1442, "20101");
        using var _ = db;

        var act = () => service.CreateShipmentAsync(new CreateShipmentRequest(
            OrderId: Guid.NewGuid(), SubOrderId: Guid.NewGuid(), SellerId: seller, RecipientName: "A", RecipientPhone: "0912345678",
            ShippingAddress: "12 Nguyen Hue", ToDistrictId: 1442, ToWardCode: "20101"));

        (await act.Should().ThrowAsync<BusinessRuleValidationException>()).Which.ErrorCode.Should().Be("WAREHOUSE_ADDRESS_INCOMPLETE");
        gateway.Calls.Should().Be(0);
    }

    // ---- fixes after the review
    [Fact]
    public async Task A_placeholder_made_earlier_is_completed_by_the_first_confirmed_address_that_arrives()
    {
        using var db = NewDb();
        var seller = Guid.NewGuid();
        db.Warehouses.Add(new Warehouse { Id = Guid.NewGuid(), SellerId = seller, Code = "WH_PH", Name = "Placeholder", IsActive = true });
        await db.SaveChangesAsync();
        var locations = new AcceptingLocations();
        var service = new WarehouseService(db, locations);

        await service.CreateInternalAsync(Internal(seller));

        var saved = await db.Warehouses.SingleAsync();
        saved.ProvinceId.Should().Be(202);
        saved.DistrictId.Should().Be(1442);
        saved.WardCode.Should().Be("20101");
        locations.Asked.Should().ContainSingle();
    }

    [Fact]
    public async Task A_complete_warehouse_is_returned_as_it_is_and_the_lists_are_not_asked()
    {
        using var db = NewDb();
        var seller = Guid.NewGuid();
        db.Warehouses.Add(new Warehouse { Id = Guid.NewGuid(), SellerId = seller, Code = "WH_OK", Name = "Done", ProvinceId = 202, DistrictId = 1442, WardCode = "20101", IsActive = true });
        await db.SaveChangesAsync();
        var locations = new AcceptingLocations { SourceDown = true };

        await new WarehouseService(db, locations).CreateInternalAsync(Internal(seller, ward: "20102"));

        (await db.Warehouses.SingleAsync()).WardCode.Should().Be("20101");
        locations.Checks.Should().Be(0);
    }

    [Fact]
    public async Task The_profile_sync_with_an_unchanged_chain_is_not_held_up_and_a_legacy_warehouse_is_completed_by_it()
    {
        using var db = NewDb();
        var seller = Guid.NewGuid();
        db.Warehouses.Add(new Warehouse { Id = Guid.NewGuid(), SellerId = seller, Code = "WH_S", Name = "Shop", ProvinceId = 202, DistrictId = 1442, WardCode = "20101", IsActive = true });
        await db.SaveChangesAsync();
        var locations = new AcceptingLocations { SourceDown = true };
        var service = new WarehouseService(db, locations);

        // same chain, new hotline, GHN unreachable: saved without asking
        var same = new InternalUpdateSellerWarehouseRequest("Shop", "0911111111", "1 Le Loi", 202, 1442, "20101");
        await service.UpdateBySellerIdAsync(seller, same);
        (await db.Warehouses.SingleAsync()).ContactPhone.Should().Be("0911111111");
        locations.Checks.Should().Be(0);

        // a legacy warehouse (no province id) receives the whole chain: it differs from what is saved, so it is checked and completed
        var warehouse = await db.Warehouses.SingleAsync();
        warehouse.ProvinceId = null;
        await db.SaveChangesAsync();
        locations.SourceDown = false;
        await service.UpdateBySellerIdAsync(seller, same);
        (await db.Warehouses.SingleAsync()).ProvinceId.Should().Be(202);
        locations.Checks.Should().Be(1);
    }

    [Fact]
    public async Task An_empty_province_name_from_a_form_does_not_overwrite_the_confirmed_name()
    {
        using var db = NewDb();
        var seller = Guid.NewGuid();
        var service = new WarehouseService(db, new AcceptingLocations());
        await service.CreateInternalAsync(Internal(seller));
        var id = (await db.Warehouses.SingleAsync()).Id;

        await service.UpdateAsync(id, new UpdateWarehouseRequest("Shop", ProvinceName: "", ProvinceId: 202, DistrictId: 1442, WardCode: "20102"), seller);

        (await db.Warehouses.SingleAsync()).ProvinceName.Should().Be("Ho Chi Minh");
    }
}
