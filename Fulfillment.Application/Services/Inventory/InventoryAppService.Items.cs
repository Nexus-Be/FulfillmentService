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
        ValidateAdjustmentParameters(request.Type, request.Quantity);

        var warehouse = await ResolveWarehouseForAdjustmentAsync(request.WarehouseId, currentUserId, isAdmin, cancellationToken);

        InventoryItem? existingItem = warehouse != null
            ? await _dbContext.InventoryItems.FirstOrDefaultAsync(i => i.SkuId == request.SkuId && i.WarehouseId == warehouse.Id, cancellationToken)
            : await _dbContext.InventoryItems.FirstOrDefaultAsync(i => i.SkuId == request.SkuId, cancellationToken);

        var (item, ledger) = ApplyAdjustment(existingItem, warehouse, request.SkuId, request.Type, request.Quantity, request.Note);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdjustInventoryResponse(
            item.SkuId,
            item.TotalQuantity,
            item.ReservedQuantity,
            item.AvailableQuantity ?? 0,
            ledger.Id);
    }

    public async Task<BatchAdjustInventoryResponse> BatchAdjustInventoryAsync(BatchAdjustInventoryRequest request, CancellationToken cancellationToken = default)
    {
        return await BatchAdjustInventoryAsync(request, Guid.Empty, isAdmin: true, cancellationToken);
    }

    public async Task<BatchAdjustInventoryResponse> BatchAdjustInventoryAsync(BatchAdjustInventoryRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            throw new BusinessRuleValidationException("VAL_INVALID_INPUT", "At least one inventory adjustment item is required.");
        }

        foreach (var entry in request.Items)
        {
            ValidateAdjustmentParameters(entry.Type, entry.Quantity);
        }

        var warehouse = await ResolveWarehouseForAdjustmentAsync(request.WarehouseId, currentUserId, isAdmin, cancellationToken);

        var distinctSkuIds = request.Items.Select(x => x.SkuId).Distinct().ToList();
        var itemQuery = _dbContext.InventoryItems.Where(i => distinctSkuIds.Contains(i.SkuId));
        if (warehouse != null)
        {
            itemQuery = itemQuery.Where(i => i.WarehouseId == warehouse.Id);
        }

        var existingItems = await itemQuery.ToListAsync(cancellationToken);
        var itemsBySku = existingItems
            .GroupBy(i => i.SkuId)
            .ToDictionary(g => g.Key, g => g.First());

        var responses = new List<AdjustInventoryResponse>(request.Items.Count);
        var resolvedWarehouseId = warehouse?.Id ?? Guid.Empty;

        foreach (var entry in request.Items)
        {
            itemsBySku.TryGetValue(entry.SkuId, out var existingItem);
            var effectiveNote = !string.IsNullOrWhiteSpace(entry.Note) ? entry.Note : request.Note;

            var (item, ledger) = ApplyAdjustment(existingItem, warehouse, entry.SkuId, entry.Type, entry.Quantity, effectiveNote);
            itemsBySku[entry.SkuId] = item;

            if (resolvedWarehouseId == Guid.Empty)
            {
                resolvedWarehouseId = item.WarehouseId;
            }

            responses.Add(new AdjustInventoryResponse(
                item.SkuId,
                item.TotalQuantity,
                item.ReservedQuantity,
                item.AvailableQuantity ?? 0,
                ledger.Id));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new BatchAdjustInventoryResponse(
            resolvedWarehouseId,
            responses.Count,
            responses);
    }

    private async Task<Warehouse?> ResolveWarehouseForAdjustmentAsync(
        Guid? requestedWarehouseId,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        if (requestedWarehouseId.HasValue && requestedWarehouseId.Value != Guid.Empty)
        {
            var warehouse = await _dbContext.Warehouses
                .FirstOrDefaultAsync(w => w.Id == requestedWarehouseId.Value, cancellationToken)
                ?? throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Warehouse", requestedWarehouseId.Value);

            if (!isAdmin && currentUserId != Guid.Empty && warehouse.SellerId != currentUserId)
            {
                throw new BusinessRuleValidationException("FORBIDDEN", "You do not have permission to manage inventory in this warehouse.", 403);
            }

            return warehouse;
        }

        if (!isAdmin && currentUserId != Guid.Empty)
        {
            return await _dbContext.Warehouses
                .FirstOrDefaultAsync(w => w.SellerId == currentUserId && w.IsActive, cancellationToken)
                ?? throw new BusinessRuleValidationException("WAREHOUSE_NOT_FOUND", "No active warehouse found for your seller account.", 404);
        }

        return null;
    }

    private (InventoryItem Item, InventoryLedger Ledger) ApplyAdjustment(
        InventoryItem? existingItem,
        Warehouse? warehouse,
        Guid skuId,
        InventoryAdjustmentType type,
        int quantity,
        string? note)
    {
        var item = existingItem;
        if (item == null)
        {
            if (warehouse != null && type == InventoryAdjustmentType.Intake && quantity > 0)
            {
                item = InventoryItem.Create(skuId, warehouse.Id, 0);
                _dbContext.InventoryItems.Add(item);
            }
            else
            {
                throw new EntityNotFoundException("INVENTORY_NOT_FOUND", "InventoryItem for SKU", skuId);
            }
        }

        item.Adjust(quantity);

        var movementType = type switch
        {
            InventoryAdjustmentType.Intake => InventoryMovementType.Intake.ToString().ToUpperInvariant(),
            InventoryAdjustmentType.Adjust => InventoryMovementType.Adjust.ToString().ToUpperInvariant(),
            _ => throw new BusinessRuleValidationException("Unsupported adjustment type.")
        };

        var ledger = InventoryLedger.Create(
            item.Id,
            movementType,
            quantity,
            note: note);

        _dbContext.InventoryLedgers.Add(ledger);

        return (item, ledger);
    }

    private static void ValidateAdjustmentParameters(InventoryAdjustmentType type, int quantity)
    {
        if (type == InventoryAdjustmentType.Intake && quantity <= 0)
        {
            throw new BusinessRuleValidationException("Quantity for INTAKE must be greater than zero.");
        }

        if (type == InventoryAdjustmentType.Adjust && quantity == 0)
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
