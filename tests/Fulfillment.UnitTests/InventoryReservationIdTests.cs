namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Fulfillment.Application.Services.Inventory;
using Fulfillment.Domain.Entities;
using Fulfillment.Infrastructure.Persistence;

/// <summary>
/// Regression for D-01: the reservation key has a database default, so EF left <c>reservation.Id</c> as Guid.Empty while the reservation
/// items copied it. On PostgreSQL the items then pointed at a parent that did not exist (FK 23503, HTTP 500) and no stock was ever held.
/// The in-memory provider generates keys itself, so the assertion is made on the entity Id directly.
/// </summary>
public class InventoryReservationIdTests
{
    [Fact]
    public void Create_AssignsANonEmptyId_BeforeAnythingIsSaved()
    {
        var reservation = InventoryReservation.Create("ORD-ID-1");

        reservation.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_AssignsDistinctIds()
    {
        var first = InventoryReservation.Create("ORD-ID-A");
        var second = InventoryReservation.Create("ORD-ID-B");

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public async Task ReserveStock_ReservationItemsPointToTheReservationThatWasSaved()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var warehouseId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        db.InventoryItems.Add(InventoryItem.Create(skuId, warehouseId, 10));
        await db.SaveChangesAsync();

        var service = new InventoryAppService(db);
        var request = new CreateReservationRequest(
            ReferenceId: "ORD-ID-RESERVE",
            Items: new List<ReserveInventoryItemRequest> { new(skuId, 3) },
            HoldMinutes: 15);

        await service.CreateReservationAsync(request);

        var reservation = await db.InventoryReservations.Include(r => r.ReservationItems).SingleAsync();
        reservation.Id.Should().NotBe(Guid.Empty);
        reservation.ReservationItems.Should().ContainSingle()
            .Which.ReservationId.Should().Be(reservation.Id);
        (await db.InventoryItems.SingleAsync()).ReservedQuantity.Should().Be(3);
    }
}
