namespace Fulfillment.Application.Services.Inventory;

using System;
using Fulfillment.Application.Interfaces;
using Fulfillment.Application.Interfaces.AI;
using Fulfillment.Application.Interfaces.Excel;
using Fulfillment.Domain.Entities;

public sealed partial class InventoryAppService : IInventoryAppService
{
    private readonly IFulfillmentDbContext _dbContext;
    private readonly IAiSchemaInferenceService? _aiSchemaInferenceService;
    private readonly IInventoryExcelParser? _excelParser;
    private readonly IInventoryExcelExporter? _excelExporter;

    public InventoryAppService(
        IFulfillmentDbContext dbContext,
        IAiSchemaInferenceService? aiSchemaInferenceService = null,
        IInventoryExcelParser? excelParser = null,
        IInventoryExcelExporter? excelExporter = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _aiSchemaInferenceService = aiSchemaInferenceService;
        _excelParser = excelParser;
        _excelExporter = excelExporter;
    }

    private static InventoryItemResponse MapToResponse(InventoryItem item) =>
        new InventoryItemResponse(
            item.Id,
            item.SkuId,
            item.WarehouseId,
            item.TotalQuantity,
            item.ReservedQuantity,
            item.AvailableQuantity ?? (item.TotalQuantity - item.ReservedQuantity),
            item.UpdatedAt,
            item.SoldQuantity,
            item.MinStockThreshold,
            item.IsLowStockAlertEnabled,
            item.IsLowStock,
            item.LastMovementAt);
}
