namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;

public sealed partial class InventoryAppService
{
    public async Task<InventoryItemResponse> DeclareInventoryAsync(DeclareInventoryRequest request, CancellationToken cancellationToken = default)
    {
        return await DeclareInventoryAsync(request, Guid.Empty, isAdmin: true, cancellationToken);
    }

    public async Task<InventoryItemResponse> DeclareInventoryAsync(DeclareInventoryRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId, cancellationToken)
            ?? throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Warehouse", request.WarehouseId);

        if (!isAdmin && currentUserId != Guid.Empty && warehouse.SellerId != currentUserId)
        {
            throw new BusinessRuleValidationException("FORBIDDEN", "You do not have permission to manage inventory in this warehouse.", 403);
        }

        var existing = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(i => i.SkuId == request.SkuId && i.WarehouseId == request.WarehouseId, cancellationToken);

        if (existing != null)
        {
            return MapToResponse(existing);
        }

        var item = InventoryItem.Create(request.SkuId, request.WarehouseId, request.Quantity);

        _dbContext.InventoryItems.Add(item);

        if (request.Quantity > 0)
        {
            var ledger = InventoryLedger.Create(
                item.Id,
                InventoryMovementType.Initial.ToString().ToUpperInvariant(),
                request.Quantity,
                note: request.Note ?? "Initial inventory declaration.");

            _dbContext.InventoryLedgers.Add(ledger);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(item);
    }

    public async Task<AdjustInventoryResponse> AdjustInventoryAsync(AdjustInventoryRequest request, CancellationToken cancellationToken = default)
    {
        return await AdjustInventoryAsync(request, Guid.Empty, isAdmin: true, cancellationToken);
    }

    public async Task<AdjustInventoryResponse> AdjustInventoryAsync(AdjustInventoryRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        ValidateAdjustRequest(request);

        Warehouse? warehouse = null;
        if (request.WarehouseId.HasValue && request.WarehouseId.Value != Guid.Empty)
        {
            warehouse = await _dbContext.Warehouses
                .FirstOrDefaultAsync(w => w.Id == request.WarehouseId.Value, cancellationToken)
                ?? throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Warehouse", request.WarehouseId.Value);

            if (!isAdmin && currentUserId != Guid.Empty && warehouse.SellerId != currentUserId)
            {
                throw new BusinessRuleValidationException("FORBIDDEN", "You do not have permission to manage inventory in this warehouse.", 403);
            }
        }
        else if (!isAdmin && currentUserId != Guid.Empty)
        {
            warehouse = await _dbContext.Warehouses
                .FirstOrDefaultAsync(w => w.SellerId == currentUserId && w.IsActive, cancellationToken)
                ?? throw new BusinessRuleValidationException("WAREHOUSE_NOT_FOUND", "No active warehouse found for your seller account.", 404);
        }

        InventoryItem? item = null;
        if (warehouse != null)
        {
            item = await _dbContext.InventoryItems
                .FirstOrDefaultAsync(i => i.SkuId == request.SkuId && i.WarehouseId == warehouse.Id, cancellationToken);
        }
        else
        {
            item = await _dbContext.InventoryItems
                .FirstOrDefaultAsync(i => i.SkuId == request.SkuId, cancellationToken);
        }

        if (item == null)
        {
            throw new EntityNotFoundException("INVENTORY_NOT_FOUND", "InventoryItem for SKU", request.SkuId);
        }

        item.Adjust(request.Quantity);

        var movementType = request.Type switch
        {
            InventoryAdjustmentType.Intake => InventoryMovementType.Intake.ToString().ToUpperInvariant(),
            InventoryAdjustmentType.Adjust => InventoryMovementType.Adjust.ToString().ToUpperInvariant(),
            _ => throw new BusinessRuleValidationException("Unsupported adjustment type.")
        };

        var ledger = InventoryLedger.Create(
            item.Id,
            movementType,
            request.Quantity,
            note: request.Note);

        _dbContext.InventoryLedgers.Add(ledger);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdjustInventoryResponse(
            item.SkuId,
            item.TotalQuantity,
            item.ReservedQuantity,
            item.AvailableQuantity ?? 0,
            ledger.Id);
    }

    private static void ValidateAdjustRequest(AdjustInventoryRequest request)
    {
        if (request.Type == InventoryAdjustmentType.Intake && request.Quantity <= 0)
        {
            throw new BusinessRuleValidationException("Quantity for INTAKE must be greater than zero.");
        }

        if (request.Type == InventoryAdjustmentType.Adjust && request.Quantity == 0)
        {
            throw new BusinessRuleValidationException("Quantity for ADJUST cannot be zero.");
        }
    }

    public async Task<InventoryItemResponse?> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.SkuId == skuId, cancellationToken);

        return item == null ? null : MapToResponse(item);
    }

    public async Task<IReadOnlyList<InventoryItemResponse>> GetByListSkuAsync(IReadOnlyList<Guid> skuIds, CancellationToken cancellationToken = default)
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

        return items.Select(MapToResponse).ToList();
    }
}
