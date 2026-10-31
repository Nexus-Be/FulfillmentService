namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Interfaces;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;

public sealed class InventoryService : IInventoryService
{
    private readonly IFulfillmentDbContext _dbContext;

    public InventoryService(IFulfillmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryItemResponse> InitializeInventoryAsync(
        InitializeInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || request.SkuId == Guid.Empty)
        {
            throw new BusinessRuleValidationException("VAL_INVALID_INPUT", "SkuId is required to initialize inventory.");
        }

        var existing = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(i => i.SkuId == request.SkuId, cancellationToken);

        if (existing != null)
        {
            return new InventoryItemResponse(
                existing.Id,
                existing.SkuId,
                existing.WarehouseId,
                existing.TotalQuantity,
                existing.AvailableQuantity,
                existing.ReservedQuantity,
                existing.UnavailableQuantity);
        }

        Guid warehouseId;
        if (request.WarehouseId.HasValue && request.WarehouseId.Value != Guid.Empty)
        {
            warehouseId = request.WarehouseId.Value;
        }
        else
        {
            var defaultWarehouse = await _dbContext.Warehouses
                .FirstOrDefaultAsync(w => !w.IsDisabled, cancellationToken);

            if (defaultWarehouse == null)
            {
                throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Warehouse", "DefaultActive");
            }

            warehouseId = defaultWarehouse.Id;
        }

        var quantity = request.InitialQuantity >= 0 ? request.InitialQuantity : 0;
        var newItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            SkuId = request.SkuId,
            WarehouseId = warehouseId,
            TotalQuantity = quantity,
            AvailableQuantity = quantity,
            ReservedQuantity = 0,
            UnavailableQuantity = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.InventoryItems.Add(newItem);

        if (quantity > 0)
        {
            var ledger = new InventoryLedger
            {
                Id = Guid.NewGuid(),
                InventoryItemId = newItem.Id,
                MovementType = InventoryMovementType.Inbound,
                Quantity = quantity,
                ReferenceType = InventoryLedgerReferenceType.Manual,
                Note = "Initial inventory declaration.",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.InventoryLedgers.Add(ledger);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new InventoryItemResponse(
            newItem.Id,
            newItem.SkuId,
            newItem.WarehouseId,
            newItem.TotalQuantity,
            newItem.AvailableQuantity,
            newItem.ReservedQuantity,
            newItem.UnavailableQuantity);
    }

    public async Task<InventoryItemResponse> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.SkuId == skuId, cancellationToken)
            ?? throw new EntityNotFoundException("INVENTORY_NOT_FOUND", "InventoryItem", skuId);

        return new InventoryItemResponse(
            item.Id,
            item.SkuId,
            item.WarehouseId,
            item.TotalQuantity,
            item.AvailableQuantity,
            item.ReservedQuantity,
            item.UnavailableQuantity);
    }

    public async Task<IReadOnlyList<InventoryItemResponse>> GetByListSkuAsync(
        IReadOnlyList<Guid> skuIds,
        CancellationToken cancellationToken = default)
    {
        if (skuIds == null || skuIds.Count == 0)
        {
            return Array.Empty<InventoryItemResponse>();
        }

        var distinctSkuIds = skuIds.Distinct().ToList();

        var items = await _dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => distinctSkuIds.Contains(i.SkuId))
            .ToListAsync(cancellationToken);

        return items.Select(item => new InventoryItemResponse(
            item.Id,
            item.SkuId,
            item.WarehouseId,
            item.TotalQuantity,
            item.AvailableQuantity,
            item.ReservedQuantity,
            item.UnavailableQuantity)).ToList();
    }
}
