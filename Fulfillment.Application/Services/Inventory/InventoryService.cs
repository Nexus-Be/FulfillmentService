namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Interfaces;
using Fulfillment.Domain.Constants;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;

public class InventoryService : IInventoryService, IInventoryAppService
{
    private readonly IFulfillmentDbContext _dbContext;

    public InventoryService(IFulfillmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private sealed record AggregatedReservationItem(Guid SkuId, int Quantity);

    #region Items

    public async Task<InventoryItemResponse> DeclareInventoryAsync(DeclareInventoryRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(i => i.SkuId == request.SkuId && i.WarehouseId == request.WarehouseId, cancellationToken);

        if (existing != null)
        {
            throw new BusinessRuleValidationException($"Inventory for SKU '{request.SkuId}' already declared in warehouse '{request.WarehouseId}'.");
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
        ValidateAdjustRequest(request);

        var item = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(i => i.SkuId == request.SkuId, cancellationToken)
            ?? throw new EntityNotFoundException("InventoryItem for SKU", request.SkuId);

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

    private static InventoryItemResponse MapToResponse(InventoryItem item) =>
        new InventoryItemResponse(
            item.Id,
            item.SkuId,
            item.WarehouseId,
            item.TotalQuantity,
            item.ReservedQuantity,
            item.AvailableQuantity ?? (item.TotalQuantity - item.ReservedQuantity),
            item.UpdatedAt);

    #endregion

    #region Ledgers

    public async Task<InventoryLedgerResponse> RecordLedgerEntryAsync(RecordLedgerRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(i => i.SkuId == request.SkuId, cancellationToken)
            ?? throw new EntityNotFoundException("InventoryItem for SKU", request.SkuId);

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
            ledger.CreatedAt);
    }

    public async Task<IReadOnlyList<InventoryLedgerResponse>> GetLedgersBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.SkuId == skuId, cancellationToken)
            ?? throw new EntityNotFoundException("InventoryItem for SKU", skuId);

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
            l.CreatedAt)).ToList();
    }

    #endregion

    #region Reservations

    public async Task<InventoryReservationResponse> CreateReservationAsync(CreateReservationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new BusinessRuleValidationException("Reservation must contain at least one item.");
        }

        var existing = await _dbContext.InventoryReservations
            .Include(r => r.ReservationItems)
            .FirstOrDefaultAsync(r => r.ReferenceId == request.ReferenceId, cancellationToken);

        if (existing != null)
        {
            return await MapToReservationResponseAsync(existing, cancellationToken);
        }

        var aggregatedRequests = AggregateReservationItems(request.Items);
        var skuIds = aggregatedRequests.Select(x => x.SkuId).ToList();
        var itemMap = await GetInventoryItemMapBySkuIdsAsync(skuIds, cancellationToken);

        var failures = new List<ReservationItemFailure>();

        foreach (var req in aggregatedRequests)
        {
            if (!itemMap.TryGetValue(req.SkuId, out var item))
            {
                failures.Add(new ReservationItemFailure(
                    req.SkuId,
                    ReservationItemErrorCodes.SkuNotFound,
                    req.Quantity,
                    0,
                    $"Inventory item for SKU '{req.SkuId}' was not found in warehouse inventory."));
                continue;
            }

            var available = item.TotalQuantity - item.ReservedQuantity;
            if (available < req.Quantity)
            {
                failures.Add(new ReservationItemFailure(
                    req.SkuId,
                    ReservationItemErrorCodes.InsufficientStock,
                    req.Quantity,
                    available,
                    $"Requested quantity ({req.Quantity}) exceeds available stock ({available}) for SKU '{req.SkuId}'."));
            }
        }

        if (failures.Count > 0)
        {
            throw new InsufficientInventoryBatchException(failures);
        }

        var reservation = InventoryReservation.Create(request.ReferenceId, request.HoldMinutes);
        _dbContext.InventoryReservations.Add(reservation);

        var reservationItems = HoldStockAndCreateReservationItems(reservation.Id, aggregatedRequests, itemMap);

        _dbContext.InventoryReservationItems.AddRange(reservationItems);
        await _dbContext.SaveChangesAsync(cancellationToken);

        reservation.ReservationItems = reservationItems;
        return await MapToReservationResponseAsync(reservation, cancellationToken);
    }

    public async Task<InventoryReservationResponse> CommitReservationAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        var reservation = await _dbContext.InventoryReservations
            .Include(r => r.ReservationItems)
            .FirstOrDefaultAsync(r => r.ReferenceId == referenceId, cancellationToken);

        if (reservation == null)
        {
            var committed = await TryGetCommittedReservationAsync(referenceId, cancellationToken);
            if (committed != null)
            {
                return committed;
            }

            throw new EntityNotFoundException("InventoryReservation for reference", referenceId);
        }

        var itemIds = reservation.ReservationItems.Select(ri => ri.InventoryItemId).Distinct().ToList();
        var items = await _dbContext.InventoryItems
            .Where(i => itemIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        var itemMap = items.ToDictionary(i => i.Id);
        var ledgers = new List<InventoryLedger>();

        foreach (var resItem in reservation.ReservationItems)
        {
            if (itemMap.TryGetValue(resItem.InventoryItemId, out var item))
            {
                item.CommitHold(resItem.Quantity);

                var ledger = InventoryLedger.Create(
                    item.Id,
                    InventoryMovementTypes.Commit,
                    resItem.Quantity,
                    referenceType: InventoryReferenceTypes.Order,
                    referenceId: referenceId,
                    note: $"Committed {resItem.Quantity} for order {referenceId}",
                    reservationId: reservation.Id);

                ledgers.Add(ledger);
            }
        }

        var earliestLedger = ledgers.MinBy(l => l.CreatedAt);
        var committedAt = earliestLedger?.CreatedAt ?? DateTimeOffset.UtcNow;

        var response = await MapToReservationResponseAsync(reservation, cancellationToken);
        var committedResponse = response with
        {
            CreatedAt = committedAt
        };

        _dbContext.InventoryLedgers.AddRange(ledgers);
        _dbContext.InventoryReservations.Remove(reservation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return committedResponse;
    }

    public async Task<InventoryReservationResponse> ReleaseReservationAsync(string referenceId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var reservation = await _dbContext.InventoryReservations
            .Include(r => r.ReservationItems)
            .FirstOrDefaultAsync(r => r.ReferenceId == referenceId, cancellationToken);

        if (reservation == null)
        {
            var committed = await TryGetCommittedReservationAsync(referenceId, cancellationToken);
            if (committed != null)
            {
                throw new ReservationAlreadyCommittedException(
                    referenceId,
                    committed.CreatedAt,
                    committed);
            }

            return InventoryReservationResponse.AlreadyReleased(referenceId);
        }

        await ReleaseHoldItemsAsync(reservation.ReservationItems, cancellationToken);

        var response = await MapToReservationResponseAsync(reservation, cancellationToken);

        _dbContext.InventoryReservations.Remove(reservation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<int> AutoExpireReservationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredReservations = await _dbContext.InventoryReservations
            .Include(r => r.ReservationItems)
            .Where(r => r.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expiredReservations.Count == 0)
        {
            return 0;
        }

        var allItems = expiredReservations.SelectMany(r => r.ReservationItems).ToList();
        await ReleaseHoldItemsAsync(allItems, cancellationToken);

        _dbContext.InventoryReservations.RemoveRange(expiredReservations);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return expiredReservations.Count;
    }

    private static IReadOnlyList<AggregatedReservationItem> AggregateReservationItems(IReadOnlyList<ReserveInventoryItemRequest> items)
    {
        return items
            .GroupBy(x => x.SkuId)
            .Select(g => new AggregatedReservationItem(g.Key, g.Sum(x => x.Quantity)))
            .OrderBy(x => x.SkuId)
            .ToList();
    }

    private async Task<Dictionary<Guid, InventoryItem>> GetInventoryItemMapBySkuIdsAsync(
        IReadOnlyList<Guid> skuIds,
        CancellationToken cancellationToken)
    {
        var items = await _dbContext.InventoryItems
            .Where(i => skuIds.Contains(i.SkuId))
            .ToListAsync(cancellationToken);

        return items.ToDictionary(i => i.SkuId);
    }

    private static List<InventoryReservationItem> HoldStockAndCreateReservationItems(
        Guid reservationId,
        IReadOnlyList<AggregatedReservationItem> requests,
        IReadOnlyDictionary<Guid, InventoryItem> itemMap)
    {
        var reservationItems = new List<InventoryReservationItem>(requests.Count);

        foreach (var req in requests)
        {
            var item = itemMap[req.SkuId];
            item.Hold(req.Quantity);

            reservationItems.Add(new InventoryReservationItem
            {
                ReservationId = reservationId,
                InventoryItemId = item.Id,
                Quantity = req.Quantity
            });
        }

        return reservationItems;
    }

    private async Task<InventoryReservationResponse?> TryGetCommittedReservationAsync(string referenceId, CancellationToken cancellationToken)
    {
        var pastLedgers = await _dbContext.InventoryLedgers
            .Where(l => l.ReferenceId == referenceId && l.MovementType == InventoryMovementTypes.Commit)
            .ToListAsync(cancellationToken);

        if (pastLedgers.Count == 0)
        {
            return null;
        }

        var pastItemIds = pastLedgers.Select(l => l.InventoryItemId).Distinct().ToList();
        var pastItems = await _dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => pastItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.SkuId, cancellationToken);

        return InventoryReservationResponse.FromCommittedLedgers(referenceId, pastLedgers, pastItems);
    }

    private async Task ReleaseHoldItemsAsync(IEnumerable<InventoryReservationItem> reservationItems, CancellationToken cancellationToken)
    {
        var itemsList = reservationItems.ToList();
        if (itemsList.Count == 0)
        {
            return;
        }

        var itemIds = itemsList.Select(ri => ri.InventoryItemId).Distinct().ToList();
        var items = await _dbContext.InventoryItems
            .Where(i => itemIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        var itemMap = items.ToDictionary(i => i.Id);

        foreach (var resItem in itemsList)
        {
            if (itemMap.TryGetValue(resItem.InventoryItemId, out var item))
            {
                item.ReleaseHold(resItem.Quantity);
            }
        }
    }

    private async Task<InventoryReservationResponse> MapToReservationResponseAsync(InventoryReservation r, CancellationToken cancellationToken)
    {
        var itemIds = r.ReservationItems.Select(ri => ri.InventoryItemId).Distinct().ToList();
        var items = await _dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        var idToSku = items.ToDictionary(i => i.Id, i => i.SkuId);

        var itemResponses = r.ReservationItems
            .Select(ri => new InventoryReservationItemResponse(
                idToSku.TryGetValue(ri.InventoryItemId, out var sku) ? sku : Guid.Empty,
                ri.Quantity))
            .ToList();

        return new InventoryReservationResponse(
            r.Id,
            r.ReferenceId,
            r.ExpiresAt,
            r.CreatedAt,
            itemResponses);
    }

    #endregion
}

public class InventoryAppService : InventoryService
{
    public InventoryAppService(IFulfillmentDbContext dbContext) : base(dbContext)
    {
    }
}
