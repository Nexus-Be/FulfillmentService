namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Fulfillment.Application.Services.Inventory;
using Fulfillment.Domain.Entities;
using Fulfillment.Infrastructure.Excel;
using Fulfillment.Infrastructure.Persistence;

public class InventoryLedgerAdvancedTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly InventoryAppService _service;
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _sku1 = Guid.NewGuid();
    private readonly Guid _sku2 = Guid.NewGuid();

    public InventoryLedgerAdvancedTests()
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
            Code = "WH_LEDGER_TEST",
            Name = "Ledger Test Warehouse",
            IsActive = true
        });

        // Seed inventory items
        var item1 = new InventoryItem
        {
            Id = Guid.NewGuid(),
            WarehouseId = _warehouseId,
            SkuId = _sku1,
            TotalQuantity = 100,
            ReservedQuantity = 10,
            AvailableQuantity = 90
        };

        var item2 = new InventoryItem
        {
            Id = Guid.NewGuid(),
            WarehouseId = _warehouseId,
            SkuId = _sku2,
            TotalQuantity = 50,
            ReservedQuantity = 0,
            AvailableQuantity = 50
        };

        _dbContext.InventoryItems.AddRange(item1, item2);

        // Seed diverse ledger movements
        _dbContext.InventoryLedgers.AddRange(
            new InventoryLedger
            {
                Id = Guid.NewGuid(),
                InventoryItemId = item1.Id,
                MovementType = "INTAKE",
                Quantity = 100,
                ReferenceType = "IMPORT",
                ReferenceId = "IMP-2026-001",
                Note = "Initial stock intake",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-10)
            },
            new InventoryLedger
            {
                Id = Guid.NewGuid(),
                InventoryItemId = item1.Id,
                MovementType = "COMMIT",
                Quantity = -10,
                ReferenceType = "ORDER",
                ReferenceId = "ORD-9999",
                Note = "Customer order fulfillment",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)
            },
            new InventoryLedger
            {
                Id = Guid.NewGuid(),
                InventoryItemId = item2.Id,
                MovementType = "INTAKE",
                Quantity = 50,
                ReferenceType = "MANUAL",
                ReferenceId = "MAN-01",
                Note = "Manual stock supplement",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new InventoryLedger
            {
                Id = Guid.NewGuid(),
                InventoryItemId = item2.Id,
                MovementType = "ADJUST",
                Quantity = -5,
                ReferenceType = "AUDIT",
                ReferenceId = "AUD-01",
                Note = "Inventory discrepancy adjustment",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        );

        _dbContext.SaveChanges();

        var exporter = new InventoryExcelExporter();
        _service = new InventoryAppService(_dbContext, excelExporter: exporter);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetLedgers_WithoutFilters_ShouldReturnAllWarehouseLedgersAndAccurateSummary()
    {
        // Arrange
        var request = new InventoryLedgerFilterRequest(
            WarehouseId: _warehouseId,
            Page: 1,
            PageSize: 10);

        // Act
        var response = await _service.GetLedgersAsync(request, _sellerId, isAdmin: false);

        // Assert
        response.Summary.TotalTransactions.Should().Be(4);
        response.Summary.TotalInboundQuantity.Should().Be(150); // 100 + 50
        response.Summary.TotalOutboundQuantity.Should().Be(-15); // -10 + -5
        response.Summary.NetChange.Should().Be(135); // 150 - 15
        response.Items.Items.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetLedgers_FilterByMovementType_ShouldReturnOnlyMatchingTransactions()
    {
        // Arrange
        var request = new InventoryLedgerFilterRequest(
            WarehouseId: _warehouseId,
            MovementType: "INTAKE");

        // Act
        var response = await _service.GetLedgersAsync(request, _sellerId, isAdmin: false);

        // Assert
        response.Summary.TotalTransactions.Should().Be(2);
        response.Summary.TotalInboundQuantity.Should().Be(150);
        response.Summary.TotalOutboundQuantity.Should().Be(0);
        response.Items.Items.Should().OnlyContain(l => l.MovementType == "INTAKE");
    }

    [Fact]
    public async Task GetLedgers_FilterByReference_ShouldReturnSpecificAuditTrail()
    {
        // Arrange
        var request = new InventoryLedgerFilterRequest(
            WarehouseId: _warehouseId,
            ReferenceType: "ORDER",
            ReferenceId: "ORD-9999");

        // Act
        var response = await _service.GetLedgersAsync(request, _sellerId, isAdmin: false);

        // Assert
        response.Summary.TotalTransactions.Should().Be(1);
        response.Items.Items[0].ReferenceId.Should().Be("ORD-9999");
        response.Items.Items[0].Quantity.Should().Be(-10);
    }

    [Fact]
    public async Task GetLedgersByReference_ShouldReturnLinkedMovements()
    {
        // Act
        var ledgers = await _service.GetLedgersByReferenceAsync("ORDER", "ORD-9999", _sellerId, isAdmin: false);

        // Assert
        ledgers.Should().HaveCount(1);
        ledgers[0].ReferenceId.Should().Be("ORD-9999");
        ledgers[0].MovementType.Should().Be("COMMIT");
    }

    [Fact]
    public async Task ExportLedgersExcel_ShouldGenerateValidSpreadsheetByteArray()
    {
        // Arrange
        var request = new InventoryLedgerFilterRequest(WarehouseId: _warehouseId);

        // Act
        var excelBytes = await _service.ExportLedgersExcelAsync(request, _sellerId, isAdmin: false);

        // Assert
        excelBytes.Should().NotBeNull();
        excelBytes.Length.Should().BeGreaterThan(1000); // Valid .xlsx ZIP container

        // Verify it can be loaded by ClosedXML
        using var stream = new MemoryStream(excelBytes);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        worksheet.Should().NotBeNull();
        worksheet.Cell(1, 1).GetString().Should().Contain("BÁO CÁO SỔ CÁI");
    }
}
