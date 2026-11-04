namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Fulfillment.Application.Services.Inventory;
using Fulfillment.Domain.Entities;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class InventoryPartialReleaseTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ReleasePartialReservation_WhenCalled_ShouldReduceReservedStockForSpecificSku()
    {
        using var db = CreateDbContext();
        var sku1 = Guid.NewGuid();
        var sku2 = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        var item1 = new InventoryItem { Id = Guid.NewGuid(), SkuId = sku1, WarehouseId = warehouseId, TotalQuantity = 10, ReservedQuantity = 0 };
        var item2 = new InventoryItem { Id = Guid.NewGuid(), SkuId = sku2, WarehouseId = warehouseId, TotalQuantity = 10, ReservedQuantity = 0 };
        db.InventoryItems.AddRange(item1, item2);
        await db.SaveChangesAsync();

        var service = new InventoryAppService(db);

        var refId = "ORDER_TEST_001";
        await service.CreateReservationAsync(new CreateReservationRequest(
            refId,
            new List<ReserveInventoryItemRequest>
            {
                new(sku1, 3),
                new(sku2, 2)
            }));

        var releaseRequest = new ReleaseReservationItemsRequest(
            SubOrderId: Guid.NewGuid(),
            Reason: "SELLER_REJECTED",
            Items: new List<ReleaseReservationItem>
            {
                new(sku1, 3)
            });

        var result = await service.ReleasePartialReservationAsync(refId, releaseRequest);

        result.Status.Should().Be("PARTIALLY_RELEASED");
        result.ReleasedCount.Should().Be(1);

        var refreshedItem1 = await db.InventoryItems.FindAsync(item1.Id);
        refreshedItem1!.ReservedQuantity.Should().Be(0);

        var refreshedItem2 = await db.InventoryItems.FindAsync(item2.Id);
        refreshedItem2!.ReservedQuantity.Should().Be(2);
    }

    [Fact]
    public async Task ReleasePartialReservation_WhenAlreadyReleased_ShouldReturnIdempotentOk()
    {
        using var db = CreateDbContext();
        var service = new InventoryAppService(db);

        var releaseRequest = new ReleaseReservationItemsRequest(
            SubOrderId: Guid.NewGuid(),
            Reason: "SELLER_REJECTED",
            Items: new List<ReleaseReservationItem>
            {
                new(Guid.NewGuid(), 1)
            });

        var result = await service.ReleasePartialReservationAsync("NON_EXISTING_REF", releaseRequest);

        result.Status.Should().Be("ALREADY_RELEASED");
        result.ReleasedCount.Should().Be(0);
    }

    [Fact]
    public async Task BatchReleaseReservations_WhenCalled_ShouldReleaseAllSpecifiedReferences()
    {
        using var db = CreateDbContext();
        var sku = Guid.NewGuid();
        var item = new InventoryItem { Id = Guid.NewGuid(), SkuId = sku, WarehouseId = Guid.NewGuid(), TotalQuantity = 10, ReservedQuantity = 0 };
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();

        var service = new InventoryAppService(db);

        var ref1 = "ORDER_BATCH_1";
        var ref2 = "ORDER_BATCH_2";

        await service.CreateReservationAsync(new CreateReservationRequest(ref1, new List<ReserveInventoryItemRequest> { new(sku, 2) }));
        await service.CreateReservationAsync(new CreateReservationRequest(ref2, new List<ReserveInventoryItemRequest> { new(sku, 3) }));

        var refreshedBefore = await db.InventoryItems.FindAsync(item.Id);
        refreshedBefore!.ReservedQuantity.Should().Be(5);

        var batchRequest = new BatchReleaseReservationsRequest(
            new List<string> { ref1, ref2 },
            "PAYMENT_TIMEOUT");

        var result = await service.BatchReleaseReservationsAsync(batchRequest);

        result.ReleasedCount.Should().Be(2);
        result.FailedCount.Should().Be(0);

        var refreshedAfter = await db.InventoryItems.FindAsync(item.Id);
        refreshedAfter!.ReservedQuantity.Should().Be(0);
    }
}
