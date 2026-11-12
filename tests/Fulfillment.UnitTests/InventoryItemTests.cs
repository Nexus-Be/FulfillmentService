namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.Text.Json;
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

        // Seed default active warehouse
        _dbContext.Warehouses.Add(new Fulfillment.Domain.Entities.Warehouse
        {
            Id = _warehouseId,
            Code = "WH_DEFAULT",
            Name = "Default Warehouse",
            IsActive = true
        });
        _dbContext.SaveChanges();

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
    public async Task DeclareInventory_WhenAlreadyDeclared_ShouldReturnExistingItem()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var request = new DeclareInventoryRequest(skuId, _warehouseId, 50, "First declaration");
        var firstResponse = await _service.DeclareInventoryAsync(request);

        // Act - Idempotent declaration should not throw, returns existing item
        var secondResponse = await _service.DeclareInventoryAsync(request);

        // Assert
        secondResponse.Should().NotBeNull();
        secondResponse.Id.Should().Be(firstResponse.Id);
        secondResponse.SkuId.Should().Be(skuId);
        secondResponse.TotalQuantity.Should().Be(50);
    }

    [Fact]
    public async Task AdjustInventory_Intake_ShouldIncreaseQuantityAndWriteLedger()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        await _service.DeclareInventoryAsync(new DeclareInventoryRequest(skuId, _warehouseId, 30));

        var request = new AdjustInventoryRequest(skuId, InventoryAdjustmentType.Intake, 20, "Restock batch");

        // Act
        var response = await _service.AdjustInventoryAsync(request);

        // Assert
        response.TotalQuantity.Should().Be(50);
        response.AvailableQuantity.Should().Be(50);

        var ledger = await _dbContext.InventoryLedgers.FirstOrDefaultAsync(l => l.Id == response.AdjustmentLedgerId);
        ledger.Should().NotBeNull();
        ledger!.MovementType.Should().Be("INTAKE");
        ledger.Quantity.Should().Be(20);
        ledger.Note.Should().Be("Restock batch");
    }

    [Fact]
    public async Task AdjustInventory_WhenResultAvailableNegative_ShouldThrowException()
    {
        // Arrange
        var skuId = Guid.NewGuid();
        var declared = await _service.DeclareInventoryAsync(new DeclareInventoryRequest(skuId, _warehouseId, 10));

        // Reserve 8 items
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

    [Theory]
    [InlineData("{\"skuId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"type\":1,\"quantity\":10}", InventoryAdjustmentType.Intake)]
    [InlineData("{\"skuId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"type\":2,\"quantity\":-5}", InventoryAdjustmentType.Adjust)]
    [InlineData("{\"skuId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"type\":\"Intake\",\"quantity\":10}", InventoryAdjustmentType.Intake)]
    [InlineData("{\"skuId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"type\":\"Adjust\",\"quantity\":-5}", InventoryAdjustmentType.Adjust)]
    [InlineData("{\"skuId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"type\":\"1\",\"quantity\":10}", InventoryAdjustmentType.Intake)]
    [InlineData("{\"skuId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"type\":\"2\",\"quantity\":-5}", InventoryAdjustmentType.Adjust)]
    public void AdjustInventoryRequest_DeserializesBothNumberAndStringEnumCorrectly(string json, InventoryAdjustmentType expectedType)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var request = JsonSerializer.Deserialize<AdjustInventoryRequest>(json, options);

        request.Should().NotBeNull();
        request!.Type.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("{\"skuId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"type\":3,\"quantity\":10}")]
    [InlineData("{\"skuId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"type\":\"InvalidType\",\"quantity\":10}")]
    public void AdjustInventoryRequest_ThrowsOnInvalidTypeValue(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var act = () => JsonSerializer.Deserialize<AdjustInventoryRequest>(json, options);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public async Task BatchAdjustInventory_WhenValidMixedIntakeAndAdjust_ShouldUpdateAllAndWriteLedgers()
    {
        // Arrange
        var skuExisting1 = Guid.NewGuid();
        var skuExisting2 = Guid.NewGuid();
        var skuNewIntake = Guid.NewGuid();

        await _service.DeclareInventoryAsync(new DeclareInventoryRequest(skuExisting1, _warehouseId, 20));
        await _service.DeclareInventoryAsync(new DeclareInventoryRequest(skuExisting2, _warehouseId, 15));

        var request = new BatchAdjustInventoryRequest(
            Items: new List<BatchAdjustInventoryItemRequest>
            {
                new(skuExisting1, InventoryAdjustmentType.Intake, 30),
                new(skuExisting2, InventoryAdjustmentType.Adjust, -5, "Damaged during inspection"),
                new(skuNewIntake, InventoryAdjustmentType.Intake, 40)
            },
            Note: "Batch receipt #12",
            WarehouseId: _warehouseId);

        // Act
        var response = await _service.BatchAdjustInventoryAsync(request);

        // Assert
        response.WarehouseId.Should().Be(_warehouseId);
        response.TotalItemsAdjusted.Should().Be(3);
        response.Items.Should().HaveCount(3);

        response.Items[0].SkuId.Should().Be(skuExisting1);
        response.Items[0].TotalQuantity.Should().Be(50);

        response.Items[1].SkuId.Should().Be(skuExisting2);
        response.Items[1].TotalQuantity.Should().Be(10);

        response.Items[2].SkuId.Should().Be(skuNewIntake);
        response.Items[2].TotalQuantity.Should().Be(40);

        var ledger1 = await _dbContext.InventoryLedgers.FirstOrDefaultAsync(l => l.Id == response.Items[0].AdjustmentLedgerId);
        ledger1.Should().NotBeNull();
        ledger1!.MovementType.Should().Be("INTAKE");
        ledger1.Quantity.Should().Be(30);
        ledger1.Note.Should().Be("Batch receipt #12");

        var ledger2 = await _dbContext.InventoryLedgers.FirstOrDefaultAsync(l => l.Id == response.Items[1].AdjustmentLedgerId);
        ledger2.Should().NotBeNull();
        ledger2!.MovementType.Should().Be("ADJUST");
        ledger2.Quantity.Should().Be(-5);
        ledger2.Note.Should().Be("Damaged during inspection");
    }

    [Fact]
    public async Task BatchAdjustInventory_WhenOneItemViolatesRule_ShouldNotSaveAnyChanges()
    {
        // Arrange
        var skuValid = Guid.NewGuid();
        var skuInvalid = Guid.NewGuid();
        await _service.DeclareInventoryAsync(new DeclareInventoryRequest(skuValid, _warehouseId, 20));
        await _service.DeclareInventoryAsync(new DeclareInventoryRequest(skuInvalid, _warehouseId, 5));

        var request = new BatchAdjustInventoryRequest(
            Items: new List<BatchAdjustInventoryItemRequest>
            {
                new(skuValid, InventoryAdjustmentType.Intake, 100),
                new(skuInvalid, InventoryAdjustmentType.Adjust, -10)
            },
            Note: "Should rollback atomically",
            WarehouseId: _warehouseId);

        // Act
        var act = async () => await _service.BatchAdjustInventoryAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleValidationException>();

        var validItemAfter = await _service.GetBySkuIdAsync(skuValid);
        validItemAfter!.TotalQuantity.Should().Be(20);
    }

    [Fact]
    public async Task BatchAdjustInventory_WhenSellerDoesNotOwnWarehouse_ShouldThrowForbidden()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherSellerId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        _dbContext.Warehouses.Add(new Fulfillment.Domain.Entities.Warehouse
        {
            Id = warehouseId,
            SellerId = ownerId,
            Code = "WH_OWNER",
            Name = "Owner Warehouse",
            IsActive = true
        });
        await _dbContext.SaveChangesAsync();

        var request = new BatchAdjustInventoryRequest(
            Items: new List<BatchAdjustInventoryItemRequest>
            {
                new(Guid.NewGuid(), InventoryAdjustmentType.Intake, 10)
            },
            WarehouseId: warehouseId);

        // Act
        var act = async () => await _service.BatchAdjustInventoryAsync(request, otherSellerId, isAdmin: false);

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task BatchAdjustInventory_WhenItemsEmpty_ShouldThrowValidationException()
    {
        // Arrange
        var request = new BatchAdjustInventoryRequest(
            Items: Array.Empty<BatchAdjustInventoryItemRequest>(),
            WarehouseId: _warehouseId);

        // Act
        var act = async () => await _service.BatchAdjustInventoryAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }
}
