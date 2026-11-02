namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Fulfillment.Application.Services.Inventory;
using Fulfillment.Domain.Constants;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Exceptions;
using Fulfillment.Infrastructure.Persistence;

public class InventoryReservationTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly InventoryAppService _service;
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _sku1 = Guid.NewGuid();
    private readonly Guid _sku2 = Guid.NewGuid();

    public InventoryReservationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _service = new InventoryAppService(_dbContext);

        // Seed 2 inventory items
        _dbContext.InventoryItems.AddRange(
            InventoryItem.Create(_sku1, _warehouseId, 10),
            InventoryItem.Create(_sku2, _warehouseId, 5)
        );
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task CreateReservation_WhenStockSufficient_ShouldHoldQuantity_WithoutWritingLedger()
    {
        // Arrange
        var request = new CreateReservationRequest(
            ReferenceId: "ORD-001",
            Items: new List<ReserveInventoryItemRequest>
            {
                new(_sku1, 3),
                new(_sku2, 2)
            },
            HoldMinutes: 15);

        // Act
        var response = await _service.CreateReservationAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ReferenceId.Should().Be("ORD-001");
        response.Id.Should().NotBeEmpty();
        response.Items.Should().HaveCount(2);

        // Check InventoryItems in DB: ReservedQuantity updated, AvailableQuantity updated
        var item1 = await _dbContext.InventoryItems.FirstAsync(i => i.SkuId == _sku1);
        item1.ReservedQuantity.Should().Be(3);
        item1.AvailableQuantity.Should().Be(7);

        var item2 = await _dbContext.InventoryItems.FirstAsync(i => i.SkuId == _sku2);
        item2.ReservedQuantity.Should().Be(2);
        item2.AvailableQuantity.Should().Be(3);

        // Crucial invariant: Hold must NOT write to ledger!
        var ledgersCount = await _dbContext.InventoryLedgers.CountAsync();
        ledgersCount.Should().Be(0, "Hold temporary reservation must not pollute inventory_ledger");
    }

    [Fact]
    public async Task CreateReservation_WhenRequestHasDuplicateSkus_ShouldAggregateQuantitiesCorrectly()
    {
        // Arrange: Sku1 appears twice (qty 2 and qty 3), total 5
        var request = new CreateReservationRequest(
            ReferenceId: "ORD-DUP-SKU",
            Items: new List<ReserveInventoryItemRequest>
            {
                new(_sku1, 2),
                new(_sku1, 3)
            });

        // Act
        var response = await _service.CreateReservationAsync(request);

        // Assert
        response.Items.Should().HaveCount(1);
        response.Items[0].SkuId.Should().Be(_sku1);
        response.Items[0].Quantity.Should().Be(5);

        var item = await _dbContext.InventoryItems.FirstAsync(i => i.SkuId == _sku1);
        item.ReservedQuantity.Should().Be(5);
        item.AvailableQuantity.Should().Be(5);
    }

    [Fact]
    public async Task CreateReservation_WhenBatchHasErrors_ShouldCollectAllFailuresAndThrowBatchException()
    {
        // Arrange: Sku1 requests 20 (available is 10), FakeSku does not exist
        var fakeSku = Guid.NewGuid();
        var request = new CreateReservationRequest(
            ReferenceId: "ORD-FAIL",
            Items: new List<ReserveInventoryItemRequest>
            {
                new(_sku1, 20),
                new(fakeSku, 2)
            });

        // Act
        var act = async () => await _service.CreateReservationAsync(request);

        // Assert
        var ex = await act.Should().ThrowAsync<InsufficientInventoryBatchException>();
        ex.Which.Failures.Should().HaveCount(2);

        var sku1Failure = ex.Which.Failures.FirstOrDefault(f => f.SkuId == _sku1);
        sku1Failure.Should().NotBeNull();
        sku1Failure!.ErrorCode.Should().Be(ReservationItemErrorCodes.InsufficientStock);
        sku1Failure.AvailableQuantity.Should().Be(10);
        sku1Failure.RequestedQuantity.Should().Be(20);

        var fakeFailure = ex.Which.Failures.FirstOrDefault(f => f.SkuId == fakeSku);
        fakeFailure.Should().NotBeNull();
        fakeFailure!.ErrorCode.Should().Be(ReservationItemErrorCodes.SkuNotFound);
        fakeFailure.AvailableQuantity.Should().Be(0);

        // Ensure nothing was partially reserved
        var item1 = await _dbContext.InventoryItems.FirstAsync(i => i.SkuId == _sku1);
        item1.ReservedQuantity.Should().Be(0, "No partial hold should be saved when batch has errors");
    }

    [Fact]
    public async Task CommitReservation_WhenValid_ShouldDeductTotalStock_WriteCommitLedger_AndRemoveFromReservations()
    {
        // Arrange: Hold first
        var request = new CreateReservationRequest(
            ReferenceId: "ORD-COMMIT-1",
            Items: new List<ReserveInventoryItemRequest> { new(_sku1, 4) });
        await _service.CreateReservationAsync(request);

        // Act: Commit
        var commitResponse = await _service.CommitReservationAsync("ORD-COMMIT-1");

        // Assert
        commitResponse.Should().NotBeNull();
        commitResponse.Id.Should().NotBeEmpty();
        commitResponse.Items.Should().HaveCount(1);

        // Total should decrease by 4, Reserved should return to 0
        var item = await _dbContext.InventoryItems.FirstAsync(i => i.SkuId == _sku1);
        item.TotalQuantity.Should().Be(6);
        item.ReservedQuantity.Should().Be(0);
        item.AvailableQuantity.Should().Be(6);

        // Exactly 1 COMMIT entry in ledger
        var ledgers = await _dbContext.InventoryLedgers.Where(l => l.ReferenceId == "ORD-COMMIT-1").ToListAsync();
        ledgers.Should().HaveCount(1);
        ledgers[0].MovementType.Should().Be(InventoryMovementTypes.Commit);
        ledgers[0].Quantity.Should().Be(4);

        // Temporary reservation must be removed to free database
        var reservationInDb = await _dbContext.InventoryReservations.FirstOrDefaultAsync(r => r.ReferenceId == "ORD-COMMIT-1");
        reservationInDb.Should().BeNull("Temporary reservation must be deleted upon commit");
    }

    [Fact]
    public async Task CommitReservation_WhenRetried_ShouldBeIdempotentAndReturnPastCommitInfoFromLedger()
    {
        // Arrange: Hold and commit once
        var request = new CreateReservationRequest(
            ReferenceId: "ORD-IDEMPOTENT",
            Items: new List<ReserveInventoryItemRequest> { new(_sku1, 3) });
        await _service.CreateReservationAsync(request);
        var firstCommit = await _service.CommitReservationAsync("ORD-IDEMPOTENT");

        // Act: Call commit a second time (Network retry / duplicate queue event)
        var secondCommit = await _service.CommitReservationAsync("ORD-IDEMPOTENT");

        // Assert
        secondCommit.Should().NotBeNull();
        secondCommit.CreatedAt.Should().Be(firstCommit.CreatedAt, "Timestamp must reflect the first actual commit");
        secondCommit.Items.Should().HaveCount(1);
        secondCommit.Items[0].SkuId.Should().Be(_sku1);
        secondCommit.Items[0].Quantity.Should().Be(3);

        // Total quantity must NOT be deducted twice
        var item = await _dbContext.InventoryItems.FirstAsync(i => i.SkuId == _sku1);
        item.TotalQuantity.Should().Be(7, "Must not double-deduct inventory on retry");

        // Ledger must still have only 1 entry
        var ledgerCount = await _dbContext.InventoryLedgers.CountAsync(l => l.ReferenceId == "ORD-IDEMPOTENT");
        ledgerCount.Should().Be(1);
    }

    [Fact]
    public async Task ReleaseReservation_WhenActive_ShouldReleaseHoldAndRemoveFromReservations()
    {
        // Arrange
        var request = new CreateReservationRequest(
            ReferenceId: "ORD-RELEASE-1",
            Items: new List<ReserveInventoryItemRequest> { new(_sku1, 5) });
        await _service.CreateReservationAsync(request);

        // Act
        var response = await _service.ReleaseReservationAsync("ORD-RELEASE-1", "User cancelled checkout");

        // Assert
        response.ReferenceId.Should().Be("ORD-RELEASE-1");

        // Reserved quantity returns back to available
        var item = await _dbContext.InventoryItems.FirstAsync(i => i.SkuId == _sku1);
        item.ReservedQuantity.Should().Be(0);
        item.AvailableQuantity.Should().Be(10);

        // Reservation deleted from temp table
        var reservationInDb = await _dbContext.InventoryReservations.FirstOrDefaultAsync(r => r.ReferenceId == "ORD-RELEASE-1");
        reservationInDb.Should().BeNull();

        // Release must not write ledger
        var ledgerCount = await _dbContext.InventoryLedgers.CountAsync();
        ledgerCount.Should().Be(0);
    }

    [Fact]
    public async Task ReleaseReservation_WhenAlreadyCommitted_ShouldThrow409ConflictReservationAlreadyCommittedException()
    {
        // Arrange: Commit an order first
        var request = new CreateReservationRequest(
            ReferenceId: "ORD-ALREADY-COMMITTED",
            Items: new List<ReserveInventoryItemRequest> { new(_sku1, 2) });
        await _service.CreateReservationAsync(request);
        await _service.CommitReservationAsync("ORD-ALREADY-COMMITTED");

        // Act: Attempt to release an already committed order
        var act = async () => await _service.ReleaseReservationAsync("ORD-ALREADY-COMMITTED");

        // Assert: Must throw ReservationAlreadyCommittedException carrying past reservation info
        var ex = await act.Should().ThrowAsync<ReservationAlreadyCommittedException>();
        ex.Which.ReferenceId.Should().Be("ORD-ALREADY-COMMITTED");
        ex.Which.Reservation.Should().NotBeNull("Must carry past committed reservation details for Frontend display");
    }

    [Fact]
    public async Task AutoExpireReservations_WhenStale_ShouldReleaseHoldsAndCleanUp()
    {
        // Arrange: Create a reservation that is expired (ExpiresAt in the past)
        var staleReservation = InventoryReservation.Create("ORD-STALE", holdMinutes: 1);
        staleReservation.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5); // Expired 5 mins ago

        var item = await _dbContext.InventoryItems.FirstAsync(i => i.SkuId == _sku1);
        item.Hold(3);

        staleReservation.ReservationItems.Add(new InventoryReservationItem
        {
            ReservationId = staleReservation.Id,
            InventoryItemId = item.Id,
            Quantity = 3
        });

        _dbContext.InventoryReservations.Add(staleReservation);
        await _dbContext.SaveChangesAsync();

        // Act: Run auto-expire job
        var expiredCount = await _service.AutoExpireReservationsAsync();

        // Assert
        expiredCount.Should().Be(1);

        // Item hold released back to available
        var itemAfter = await _dbContext.InventoryItems.FirstAsync(i => i.SkuId == _sku1);
        itemAfter.ReservedQuantity.Should().Be(0);
        itemAfter.AvailableQuantity.Should().Be(10);

        // Stale reservation deleted
        var staleInDb = await _dbContext.InventoryReservations.FirstOrDefaultAsync(r => r.ReferenceId == "ORD-STALE");
        staleInDb.Should().BeNull();
    }
}
