namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Exceptions;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Exceptions;

public sealed partial class InventoryAppService
{
    public async Task<InventoryLedgerResponse> RecordLedgerEntryAsync(RecordLedgerRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.InventoryItems
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(i => i.SkuId == request.SkuId, cancellationToken)
            ?? throw new EntityNotFoundException("INVENTORY_NOT_FOUND", "InventoryItem for SKU", request.SkuId);

        var ledger = InventoryLedger.Create(
            item.Id,
            request.MovementType,
            request.Quantity,
            referenceType: request.ReferenceType,
            referenceId: request.ReferenceId,
            note: request.Note,
            reservationId: request.ReservationId);

        _dbContext.InventoryLedgers.Add(ledger);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new InventoryLedgerResponse(
            ledger.Id,
            request.SkuId,
            ledger.MovementType,
            ledger.Quantity,
            ledger.ReservationId,
            ledger.ReferenceType,
            ledger.ReferenceId,
            ledger.Note,
            ledger.CreatedAt,
            item.WarehouseId,
            item.Warehouse?.Name);
    }

    public async Task<IReadOnlyList<InventoryLedgerResponse>> GetLedgersBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.InventoryItems
            .AsNoTracking()
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(i => i.SkuId == skuId, cancellationToken)
            ?? throw new EntityNotFoundException("INVENTORY_NOT_FOUND", "InventoryItem for SKU", skuId);

        var ledgers = await _dbContext.InventoryLedgers
            .AsNoTracking()
            .Where(l => l.InventoryItemId == item.Id)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return ledgers.Select(l => new InventoryLedgerResponse(
            l.Id,
            skuId,
            l.MovementType,
            l.Quantity,
            l.ReservationId,
            l.ReferenceType,
            l.ReferenceId,
            l.Note,
            l.CreatedAt,
            item.WarehouseId,
            item.Warehouse?.Name)).ToList();
    }

    public async Task<InventoryLedgerPagedResponse> GetLedgersAsync(
        InventoryLedgerFilterRequest request,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var effectiveWarehouseId = await ResolveEffectiveWarehouseIdAsync(request.WarehouseId, currentUserId, isAdmin, cancellationToken);
        var warehouse = await _dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == effectiveWarehouseId, cancellationToken);

        var warehouseName = warehouse?.Name;

        var query = BuildLedgerQuery(request, effectiveWarehouseId);

        // 1. Calculate audit summary in single SQL aggregate
        var summaryData = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Inbound = g.Where(x => x.Quantity > 0).Sum(x => (int?)x.Quantity) ?? 0,
                Outbound = g.Where(x => x.Quantity < 0).Sum(x => (int?)x.Quantity) ?? 0,
                TotalCount = g.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        var summary = new InventoryLedgerSummaryResponse(
            TotalInboundQuantity: summaryData?.Inbound ?? 0,
            TotalOutboundQuantity: summaryData?.Outbound ?? 0,
            NetChange: (summaryData?.Inbound ?? 0) + (summaryData?.Outbound ?? 0),
            TotalTransactions: summaryData?.TotalCount ?? 0);

        // 2. Pagination on DB
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);
        var totalCount = summary.TotalTransactions;

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new InventoryLedgerResponse(
                l.Id,
                l.InventoryItem!.SkuId,
                l.MovementType,
                l.Quantity,
                l.ReservationId,
                l.ReferenceType,
                l.ReferenceId,
                l.Note,
                l.CreatedAt,
                l.InventoryItem.WarehouseId,
                l.InventoryItem.Warehouse != null ? l.InventoryItem.Warehouse.Name : warehouseName))
            .ToListAsync(cancellationToken);

        var pagedData = new PagedData<InventoryLedgerResponse>(items, PaginationMetadata.Create(page, pageSize, totalCount));
        return new InventoryLedgerPagedResponse(summary, pagedData);
    }

    public async Task<byte[]> ExportLedgersExcelAsync(
        InventoryLedgerFilterRequest request,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_excelExporter == null)
        {
            throw new InvalidOperationException("Excel exporter service is not configured.");
        }

        var effectiveWarehouseId = await ResolveEffectiveWarehouseIdAsync(request.WarehouseId, currentUserId, isAdmin, cancellationToken);
        var warehouse = await _dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == effectiveWarehouseId, cancellationToken);

        var warehouseName = warehouse?.Name;
        var query = BuildLedgerQuery(request, effectiveWarehouseId);

        // Up to 5000 rows for export
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(5000)
            .Select(l => new InventoryLedgerResponse(
                l.Id,
                l.InventoryItem!.SkuId,
                l.MovementType,
                l.Quantity,
                l.ReservationId,
                l.ReferenceType,
                l.ReferenceId,
                l.Note,
                l.CreatedAt,
                l.InventoryItem.WarehouseId,
                l.InventoryItem.Warehouse != null ? l.InventoryItem.Warehouse.Name : warehouseName))
            .ToListAsync(cancellationToken);

        var inbound = items.Where(x => x.Quantity > 0).Sum(x => x.Quantity);
        var outbound = items.Where(x => x.Quantity < 0).Sum(x => x.Quantity);
        var summary = new InventoryLedgerSummaryResponse(
            TotalInboundQuantity: inbound,
            TotalOutboundQuantity: outbound,
            NetChange: inbound + outbound,
            TotalTransactions: items.Count);

        return _excelExporter.ExportLedgersToExcel(items, summary, warehouseName);
    }

    public async Task<IReadOnlyList<InventoryLedgerResponse>> GetLedgersByReferenceAsync(
        string referenceType,
        string referenceId,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId);

        var normRefType = referenceType.Trim().ToUpperInvariant();
        var normRefId = referenceId.Trim();

        var query = _dbContext.InventoryLedgers
            .AsNoTracking()
            .Include(l => l.InventoryItem)
            .ThenInclude(i => i!.Warehouse)
            .Where(l => l.ReferenceType == normRefType && l.ReferenceId == normRefId);

        if (!isAdmin && currentUserId != Guid.Empty)
        {
            query = query.Where(l => l.InventoryItem != null && l.InventoryItem.Warehouse != null && l.InventoryItem.Warehouse.SellerId == currentUserId);
        }

        var ledgers = await query
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return ledgers.Select(l => new InventoryLedgerResponse(
            l.Id,
            l.InventoryItem!.SkuId,
            l.MovementType,
            l.Quantity,
            l.ReservationId,
            l.ReferenceType,
            l.ReferenceId,
            l.Note,
            l.CreatedAt,
            l.InventoryItem.WarehouseId,
            l.InventoryItem.Warehouse?.Name)).ToList();
    }

    private IQueryable<InventoryLedger> BuildLedgerQuery(InventoryLedgerFilterRequest request, Guid effectiveWarehouseId)
    {
        var query = _dbContext.InventoryLedgers
            .AsNoTracking()
            .Include(l => l.InventoryItem)
            .ThenInclude(i => i!.Warehouse)
            .Where(l => l.InventoryItem != null && l.InventoryItem.WarehouseId == effectiveWarehouseId);

        if (request.SkuId.HasValue && request.SkuId.Value != Guid.Empty)
        {
            query = query.Where(l => l.InventoryItem!.SkuId == request.SkuId.Value);
        }

        if (request.SkuIds != null && request.SkuIds.Count > 0)
        {
            query = query.Where(l => request.SkuIds.Contains(l.InventoryItem!.SkuId));
        }

        if (!string.IsNullOrWhiteSpace(request.MovementType))
        {
            var normMovement = request.MovementType.Trim().ToUpperInvariant();
            query = query.Where(l => l.MovementType == normMovement);
        }

        if (!string.IsNullOrWhiteSpace(request.ReferenceType))
        {
            var normRefType = request.ReferenceType.Trim().ToUpperInvariant();
            query = query.Where(l => l.ReferenceType == normRefType);
        }

        if (!string.IsNullOrWhiteSpace(request.ReferenceId))
        {
            var normRefId = request.ReferenceId.Trim();
            query = query.Where(l => l.ReferenceId != null && l.ReferenceId.Contains(normRefId));
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= request.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLower();
            query = query.Where(l =>
                (l.Note != null && l.Note.ToLower().Contains(keyword)) ||
                (l.ReferenceId != null && l.ReferenceId.ToLower().Contains(keyword)));
        }

        return query;
    }
}
