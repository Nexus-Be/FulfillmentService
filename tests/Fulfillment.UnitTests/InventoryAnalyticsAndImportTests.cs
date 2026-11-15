namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Interfaces.AI;
using Fulfillment.Application.Services.Inventory;
using Fulfillment.Domain.Entities;
using Fulfillment.Infrastructure.AI;
using Fulfillment.Infrastructure.Persistence;

public class InventoryAnalyticsAndImportTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly InventoryAppService _service;
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();

    public InventoryAnalyticsAndImportTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);

        // Seed warehouse
        _dbContext.Warehouses.Add(new Warehouse
        {
            Id = _warehouseId,
            SellerId = _sellerId,
            Code = "WH_ANALYTICS_TEST",
            Name = "Analytics Test Warehouse",
            IsActive = true
        });

        // Seed inventory items
        _dbContext.InventoryItems.AddRange(
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                WarehouseId = _warehouseId,
                SkuId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                TotalQuantity = 100,
                ReservedQuantity = 10,
                SoldQuantity = 20,
                MinStockThreshold = 15,
                IsLowStockAlertEnabled = true,
                LastMovementAt = DateTimeOffset.UtcNow.AddDays(-5)
            },
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                WarehouseId = _warehouseId,
                SkuId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                TotalQuantity = 5,
                ReservedQuantity = 0,
                SoldQuantity = 0,
                MinStockThreshold = 10,
                IsLowStockAlertEnabled = true,
                LastMovementAt = DateTimeOffset.UtcNow.AddDays(-40) // Dead stock
            },
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                WarehouseId = _warehouseId,
                SkuId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                TotalQuantity = 0,
                ReservedQuantity = 0,
                SoldQuantity = 50,
                MinStockThreshold = 10,
                IsLowStockAlertEnabled = false, // Alert disabled
                LastMovementAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        );

        _dbContext.SaveChanges();
        _service = new InventoryAppService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetLowStockAlerts_ShouldReturnOnlyEnabledItemsBelowThreshold()
    {
        // Act
        var alerts = await _service.GetLowStockAlertsAsync(_warehouseId, _sellerId, isAdmin: false);

        // Assert
        alerts.Should().NotBeNull();
        alerts.Should().HaveCount(1);
        alerts[0].SkuId.Should().Be(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        alerts[0].AvailableQuantity.Should().Be(5);
        alerts[0].MinStockThreshold.Should().Be(10);
    }

    [Fact]
    public async Task GetDeadStockAlerts_ShouldReturnActiveItemsWithoutMovementBeyondCutoff()
    {
        // Act (items inactive > 30 days)
        var deadStock = await _service.GetDeadStockAlertsAsync(_warehouseId, daysThreshold: 30, _sellerId, isAdmin: false);

        // Assert
        deadStock.Should().NotBeNull();
        deadStock.Should().HaveCount(1);
        deadStock[0].SkuId.Should().Be(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        deadStock[0].AvailableQuantity.Should().Be(5);
        deadStock[0].DaysWithoutMovement.Should().BeGreaterThanOrEqualTo(39);
    }

    [Fact]
    public async Task UpdateThreshold_WhenValid_ShouldPersistNewThresholdAndFlag()
    {
        // Arrange
        var targetSku = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var request = new UpdateStockThresholdRequest(
            MinStockThreshold: 50,
            IsLowStockAlertEnabled: true);

        // Act
        var result = await _service.UpdateThresholdAsync(targetSku, request, _sellerId, isAdmin: false);

        // Assert
        result.MinStockThreshold.Should().Be(50);
        result.IsLowStockAlertEnabled.Should().BeTrue();

        var entity = await _dbContext.InventoryItems.FirstAsync(i => i.SkuId == targetSku);
        entity.MinStockThreshold.Should().Be(50);
        entity.IsLowStockAlertEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetAnalytics_ShouldCalculateSummaryCorrectly()
    {
        // Arrange
        var filter = new InventoryAnalyticsFilterRequest(
            WarehouseId: _warehouseId,
            Page: 1,
            PageSize: 10);

        // Act
        var response = await _service.GetAnalyticsAsync(filter, _sellerId, isAdmin: false);

        // Assert
        response.Summary.TotalItems.Should().Be(3);
        response.Summary.TotalAvailableQuantity.Should().Be(95); // (100-10) + 5 + 0
        response.Summary.TotalReservedQuantity.Should().Be(10);
        response.Summary.TotalSoldQuantity.Should().Be(70); // 20 + 0 + 50
        response.Summary.LowStockCount.Should().Be(1);
        response.Summary.DeadStockCount.Should().Be(1);
        response.Items.Items.Should().HaveCount(3);
    }

    [Fact]
    public void HeuristicInference_ShouldDetectColumnsCorrectly()
    {
        // Arrange
        var sampleRows = new List<IReadOnlyList<string>>
        {
            new List<string> { "STT", "Mã SKU", "Số lượng", "Ghi chú" },
            new List<string> { "1", "SKU-001", "10", "Nhap hang" }
        };

        // Act
        var result = GeminiSchemaInferenceService.InferSchemaHeuristically(sampleRows);

        // Assert
        result.HeaderRowIndex.Should().Be(0);
        result.SkuColIndex.Should().Be(1);
        result.QuantityColIndex.Should().Be(2);
        result.NoteColIndex.Should().Be(3);
        result.Mode.Should().Be("SINGLE_QTY_COL");
    }
}
