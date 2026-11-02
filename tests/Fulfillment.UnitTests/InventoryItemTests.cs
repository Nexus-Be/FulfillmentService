namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Fulfillment.Application.Services.Inventory;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;
using Fulfillment.Infrastructure.Persistence;

public class InventoryItemTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly InventoryAppService _service;
    private readonly Guid _warehouseId = Guid.NewGuid();

    public InventoryItemTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _service = new InventoryAppService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task DeclareInventory_WhenValid_ShouldCreateItemAndInitialLedger()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var request = new DeclareInventoryRequest(skuId, _warehouseId, 50, "Initial declaration");

        // Act
        var response = await _service.DeclareInventoryAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.SkuId.Should().Be(skuId);
        response.TotalQuantity.Should().Be(50);
        response.AvailableQuantity.Should().Be(50);
        response.ReservedQuantity.Should().Be(0);

        // Ledger must record INITIAL
        var ledger = await _dbContext.InventoryLedgers.FirstOrDefaultAsync(l => l.InventoryItemId == response.Id);
        ledger.Should().NotBeNull();
        ledger!.MovementType.Should().Be("INITIAL");
        ledger.Quantity.Should().Be(50);
    }

    [Fact]
    public async Task DeclareInventory_WhenAlreadyDeclared_ShouldThrowValidationException()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var request = new DeclareInventoryRequest(skuId, _warehouseId, 50, "First declaration");
        await _service.DeclareInventoryAsync(request);

        // Act
        var act = async () => await _service.DeclareInventoryAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task AdjustInventory_Intake_ShouldIncreaseQuantityAndWriteLedger()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        await _service.DeclareInventoryAsync(new DeclareInventoryRequest(skuId, _warehouseId, 30));

        // Act: Intake 20
        var request = new AdjustInventoryRequest(skuId, InventoryAdjustmentType.Intake, 20, "Supplier restock");
        var response = await _service.AdjustInventoryAsync(request);

        // Assert
        response.TotalQuantity.Should().Be(50);
        response.AvailableQuantity.Should().Be(50);

        var ledgers = await _dbContext.InventoryLedgers.ToListAsync();
        ledgers.Should().Contain(l => l.MovementType == "INTAKE" && l.Quantity == 20);
    }

    [Fact]
    public async Task AdjustInventory_WhenAdjustBelowReserved_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange: 10 in stock, reserve 8
        var skuId = Guid.NewGuid();
        var declared = await _service.DeclareInventoryAsync(new DeclareInventoryRequest(skuId, _warehouseId, 10));

        var item = await _dbContext.InventoryItems.FirstAsync(i => i.Id == declared.Id);
        item.Hold(8);
        await _dbContext.SaveChangesAsync();

        // Act: Reduce by 5 (Total becomes 5, but Reserved is 8 => illegal!)
        var request = new AdjustInventoryRequest(skuId, InventoryAdjustmentType.Adjust, -5, "Damaged items");
        var act = async () => await _service.AdjustInventoryAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task GetBySkuId_WhenNotFound_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetBySkuIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByListSku_ShouldReturnOnlyExistingItems()
    {
        // Arrange
        var sku1 = Guid.NewGuid();
        var sku2 = Guid.NewGuid();
        var skuFake = Guid.NewGuid();

        await _service.DeclareInventoryAsync(new DeclareInventoryRequest(sku1, _warehouseId, 10));
        await _service.DeclareInventoryAsync(new DeclareInventoryRequest(sku2, _warehouseId, 20));

        // Act
        var list = await _service.GetByListSkuAsync(new List<Guid> { sku1, sku2, skuFake });

        // Assert
        list.Should().HaveCount(2);
        list.Should().Contain(i => i.SkuId == sku1);
        list.Should().Contain(i => i.SkuId == sku2);
    }
}

