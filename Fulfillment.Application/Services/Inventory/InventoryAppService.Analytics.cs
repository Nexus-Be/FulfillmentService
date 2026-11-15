namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Common;
using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Exceptions;
using Fulfillment.Domain.Exceptions;

public sealed partial class InventoryAppService
{
    public async Task<InventoryItemResponse> UpdateThresholdAsync(
        Guid skuId,
        UpdateStockThresholdRequest request,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = await _dbContext.InventoryItems
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(i => i.SkuId == skuId, cancellationToken)
            ?? throw new EntityNotFoundException("INVENTORY_NOT_FOUND", "InventoryItem for SKU", skuId);

        if (!isAdmin && currentUserId != Guid.Empty && item.Warehouse?.SellerId != currentUserId)
        {
            throw new BusinessRuleValidationException("FORBIDDEN", "You do not have permission to configure stock thresholds for this item.", 403);
        }

        item.MinStockThreshold = request.MinStockThreshold;
        if (request.IsLowStockAlertEnabled.HasValue)
        {
            item.IsLowStockAlertEnabled = request.IsLowStockAlertEnabled.Value;
        }
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(item);
    }

    public async Task<IReadOnlyList<LowStockAlertItemResponse>> GetLowStockAlertsAsync(
        Guid? warehouseId,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var effectiveWarehouseId = await ResolveEffectiveWarehouseIdAsync(warehouseId, currentUserId, isAdmin, cancellationToken);

        var items = await _dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => i.WarehouseId == effectiveWarehouseId &&
                        i.IsLowStockAlertEnabled &&
                        (i.AvailableQuantity ?? (i.TotalQuantity - i.ReservedQuantity)) <= i.MinStockThreshold)
            .OrderBy(i => i.AvailableQuantity ?? (i.TotalQuantity - i.ReservedQuantity))
            .Select(i => new LowStockAlertItemResponse(
                i.SkuId,
                i.WarehouseId,
                i.AvailableQuantity ?? (i.TotalQuantity - i.ReservedQuantity),
                i.TotalQuantity,
                i.ReservedQuantity,
                i.MinStockThreshold,
                i.LastMovementAt))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<IReadOnlyList<DeadStockAlertItemResponse>> GetDeadStockAlertsAsync(
        Guid? warehouseId,
        int daysThreshold,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var effectiveWarehouseId = await ResolveEffectiveWarehouseIdAsync(warehouseId, currentUserId, isAdmin, cancellationToken);
        var effectiveDays = daysThreshold > 0 ? daysThreshold : 30;
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-effectiveDays);

        var items = await _dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => i.WarehouseId == effectiveWarehouseId &&
                        (i.AvailableQuantity ?? (i.TotalQuantity - i.ReservedQuantity)) > 0 &&
                        i.LastMovementAt <= cutoffDate)
            .OrderBy(i => i.LastMovementAt)
            .Select(i => new DeadStockAlertItemResponse(
                i.SkuId,
                i.WarehouseId,
                i.AvailableQuantity ?? (i.TotalQuantity - i.ReservedQuantity),
                i.TotalQuantity,
                i.LastMovementAt,
                (int)(DateTimeOffset.UtcNow - i.LastMovementAt).TotalDays))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<InventoryAnalyticsResponse> GetAnalyticsAsync(
        InventoryAnalyticsFilterRequest request,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var effectiveWarehouseId = await ResolveEffectiveWarehouseIdAsync(request.WarehouseId, currentUserId, isAdmin, cancellationToken);

        var query = _dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => i.WarehouseId == effectiveWarehouseId);

        if (request.SkuIds != null && request.SkuIds.Count > 0)
        {
            query = query.Where(i => request.SkuIds.Contains(i.SkuId));
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(i => i.UpdatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(i => i.UpdatedAt <= request.ToDate.Value);
        }

        // Summary calculations
        var allItems = await query.ToListAsync(cancellationToken);
        var deadStockCutoff = DateTimeOffset.UtcNow.AddDays(-30);

        var summary = new InventoryAnalyticsSummaryResponse(
            TotalItems: allItems.Count,
            TotalStockQuantity: allItems.Sum(i => i.TotalQuantity),
            TotalAvailableQuantity: allItems.Sum(i => i.AvailableQuantity ?? (i.TotalQuantity - i.ReservedQuantity)),
            TotalReservedQuantity: allItems.Sum(i => i.ReservedQuantity),
            TotalSoldQuantity: allItems.Sum(i => i.SoldQuantity),
            LowStockCount: allItems.Count(i => i.IsLowStock),
            DeadStockCount: allItems.Count(i => (i.AvailableQuantity ?? (i.TotalQuantity - i.ReservedQuantity)) > 0 && i.LastMovementAt <= deadStockCutoff));

        // Sorting
        var isAsc = string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        var sortedQuery = request.SortBy?.ToLowerInvariant() switch
        {
            "sold_quantity" => isAsc ? query.OrderBy(i => i.SoldQuantity) : query.OrderByDescending(i => i.SoldQuantity),
            "available_quantity" => isAsc ? query.OrderBy(i => i.AvailableQuantity ?? (i.TotalQuantity - i.ReservedQuantity)) : query.OrderByDescending(i => i.AvailableQuantity ?? (i.TotalQuantity - i.ReservedQuantity)),
            "total_quantity" => isAsc ? query.OrderBy(i => i.TotalQuantity) : query.OrderByDescending(i => i.TotalQuantity),
            "last_movement_at" => isAsc ? query.OrderBy(i => i.LastMovementAt) : query.OrderByDescending(i => i.LastMovementAt),
            _ => isAsc ? query.OrderBy(i => i.UpdatedAt) : query.OrderByDescending(i => i.UpdatedAt)
        };

        var (safePage, safePageSize) = PaginationConstants.Normalize(request.Page, request.PageSize);
        var pagedItems = sortedQuery
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(i => MapToResponse(i))
            .ToList();

        var pagedData = new PagedData<InventoryItemResponse>(
            pagedItems,
            PaginationMetadata.Create(safePage, safePageSize, allItems.Count));

        return new InventoryAnalyticsResponse(summary, pagedData);
    }

    private async Task<Guid> ResolveEffectiveWarehouseIdAsync(
        Guid? requestedWarehouseId,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        if (requestedWarehouseId.HasValue && requestedWarehouseId.Value != Guid.Empty)
        {
            var warehouse = await _dbContext.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == requestedWarehouseId.Value, cancellationToken)
                ?? throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Warehouse", requestedWarehouseId.Value);

            if (!isAdmin && currentUserId != Guid.Empty && warehouse.SellerId != currentUserId)
            {
                throw new BusinessRuleValidationException("FORBIDDEN", "You do not have permission to access inventory for this warehouse.", 403);
            }

            return warehouse.Id;
        }

        if (currentUserId != Guid.Empty)
        {
            var sellerWarehouse = await _dbContext.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.SellerId == currentUserId && w.IsActive, cancellationToken)
                ?? throw new BusinessRuleValidationException("WAREHOUSE_NOT_FOUND", "No active warehouse found for current seller.", 404);

            return sellerWarehouse.Id;
        }

        var defaultWarehouse = await _dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.IsActive, cancellationToken)
            ?? throw new BusinessRuleValidationException("WAREHOUSE_NOT_FOUND", "No active warehouse available.", 404);

        return defaultWarehouse.Id;
    }
}

