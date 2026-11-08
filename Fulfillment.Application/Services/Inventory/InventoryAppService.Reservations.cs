namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Domain.Constants;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Exceptions;

public sealed partial class InventoryAppService
{
    private sealed record AggregatedReservationItem(Guid SkuId, int Quantity);

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
        var skuIds = aggregatedRequests.Select(x => x.SkuId).ToArray();
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

        // Xóa thẳng tất cả các phiếu quá hạn rác khỏi kho tạm
        _dbContext.InventoryReservations.RemoveRange(expiredReservations);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return expiredReservations.Count;
    }

    public async Task<ReleaseReservationResponse> ReleasePartialReservationAsync(
        string referenceId,
        ReleaseReservationItemsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reservation = await _dbContext.InventoryReservations
            .Include(r => r.ReservationItems)
            .FirstOrDefaultAsync(r => r.ReferenceId == referenceId, cancellationToken);

        if (reservation == null)
        {
            return new ReleaseReservationResponse(referenceId, "ALREADY_RELEASED", 0);
        }

        var skuIds = request.Items.Select(i => i.SkuId).Distinct().ToList();
        var inventoryItems = await _dbContext.InventoryItems
            .Where(i => skuIds.Contains(i.SkuId))
            .ToListAsync(cancellationToken);
        var skuToItemMap = inventoryItems.ToDictionary(i => i.SkuId);

        int releasedCount = 0;
        foreach (var reqItem in request.Items)
        {
            if (!skuToItemMap.TryGetValue(reqItem.SkuId, out var invItem))
            {
                continue;
            }

            var resItem = reservation.ReservationItems
                .FirstOrDefault(ri => ri.InventoryItemId == invItem.Id);

            if (resItem == null)
            {
                continue;
            }

            var qtyToRelease = Math.Min(resItem.Quantity, reqItem.Quantity);
            invItem.ReleaseHold(qtyToRelease);
            releasedCount++;

            if (resItem.Quantity <= qtyToRelease)
            {
                _dbContext.InventoryReservationItems.Remove(resItem);
            }
            else
            {
                resItem.Quantity -= qtyToRelease;
            }
        }

        if (reservation.ReservationItems.All(ri => ri.Quantity <= 0))
        {
            _dbContext.InventoryReservations.Remove(reservation);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ReleaseReservationResponse(referenceId, "PARTIALLY_RELEASED", releasedCount);
    }

    public async Task<BatchReleaseReservationsResponse> BatchReleaseReservationsAsync(
        BatchReleaseReservationsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ReferenceIds.Count == 0)
        {
            return new BatchReleaseReservationsResponse(0, 0);
        }

        var reservations = await _dbContext.InventoryReservations
            .Include(r => r.ReservationItems)
            .Where(r => request.ReferenceIds.Contains(r.ReferenceId))
            .ToListAsync(cancellationToken);

        if (reservations.Count == 0)
        {
            return new BatchReleaseReservationsResponse(0, 0);
        }

        var allItems = reservations.SelectMany(r => r.ReservationItems).ToList();
        await ReleaseHoldItemsAsync(allItems, cancellationToken);

        _dbContext.InventoryReservations.RemoveRange(reservations);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new BatchReleaseReservationsResponse(reservations.Count, request.ReferenceIds.Count - reservations.Count);
    }

    private static List<AggregatedReservationItem> AggregateReservationItems(IReadOnlyList<ReserveInventoryItemRequest> items)
    {
        return [.. items
            .GroupBy(x => x.SkuId)
            .Select(g => new AggregatedReservationItem(g.Key, g.Sum(x => x.Quantity)))
            .OrderBy(x => x.SkuId)];
    }

    private async Task<Dictionary<Guid, InventoryItem>> GetInventoryItemMapBySkuIdsAsync(
        IReadOnlyCollection<Guid> skuIds,
        CancellationToken cancellationToken)
    {
        var items = await _dbContext.InventoryItems
            .Where(i => skuIds.Contains(i.SkuId))
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(i => i.SkuId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private static List<InventoryReservationItem> HoldStockAndCreateReservationItems(
        Guid reservationId,
        IReadOnlyList<AggregatedReservationItem> requests,
        Dictionary<Guid, InventoryItem> itemMap)
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

    /// <summary>
    /// Tìm thông của reservation trong số cái xem đã thanh toán chưa
    /// </summary>
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
}
