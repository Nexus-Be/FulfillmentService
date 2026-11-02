namespace Fulfillment.UnitTests;

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Fulfillment.Application.Services.Inventory;
using Fulfillment.Domain.Constants;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Exceptions;
using Fulfillment.Infrastructure.Persistence;

public class InventoryLedgerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly InventoryAppService _service;
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _skuId = Guid.NewGuid();

    public InventoryLedgerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _service = new InventoryAppService(_dbContext);

        // Seed 1 inventory item
        _dbContext.InventoryItems.Add(InventoryItem.Create(_skuId, _warehouseId, 50));
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task RecordLedgerEntry_WhenValid_ShouldInsertLedgerSuccessfully()
    {
        // Arrange
        var request = new RecordLedgerRequest(
            SkuId: _skuId,
            MovementType: InventoryMovementTypes.Inbound,
            Quantity: 25,
            ReferenceType: InventoryReferenceTypes.Inbound,
            ReferenceId: "PO-12345",
            Note: "Supplier shipment inbound",
            ReservationId: null);

        // Act
        var response = await _service.RecordLedgerEntryAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.SkuId.Should().Be(_skuId);
        response.MovementType.Should().Be(InventoryMovementTypes.Inbound);
        response.Quantity.Should().Be(25);
        response.ReferenceId.Should().Be("PO-12345");

        var ledgerInDb = await _dbContext.InventoryLedgers.FirstOrDefaultAsync(l => l.ReferenceId == "PO-12345");
        ledgerInDb.Should().NotBeNull();
        ledgerInDb!.Quantity.Should().Be(25);
    }

    [Fact]
    public async Task RecordLedgerEntry_WhenSkuNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var fakeSku = Guid.NewGuid();
        var request = new RecordLedgerRequest(
            SkuId: fakeSku,
            MovementType: InventoryMovementTypes.Adjust,
            Quantity: 10);

        // Act
        var act = async () => await _service.RecordLedgerEntryAsync(request);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task GetLedgersBySkuId_ShouldReturnLedgersOrderedDescendingByCreatedAt()
    {
        // Arrange: Insert 2 ledgers with different times
        var item = await _dbContext.InventoryItems.FirstAsync(i => i.SkuId == _skuId);

        var ledger1 = InventoryLedger.Create(item.Id, "INITIAL", 50, note: "First");
        ledger1.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        var ledger2 = InventoryLedger.Create(item.Id, "INTAKE", 20, note: "Second");
        ledger2.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        _dbContext.InventoryLedgers.AddRange(ledger1, ledger2);
        await _dbContext.SaveChangesAsync();

        // Act
        var ledgers = await _service.GetLedgersBySkuIdAsync(_skuId);

        // Assert
        ledgers.Should().HaveCount(2);
        ledgers[0].Note.Should().Be("Second", "Most recent ledger must be first");
        ledgers[1].Note.Should().Be("First");
    }

    [Fact]
    public async Task GetLedgersBySkuId_WhenSkuNotFound_ShouldThrowEntityNotFoundException()
    {
        // Act
        var act = async () => await _service.GetLedgersBySkuIdAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}
