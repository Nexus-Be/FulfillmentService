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
            if (existing.IsActive)
            {
                return await MapToReservationResponseAsync(existing, cancellationToken);
            }
            throw new BusinessRuleValidationException($"Reservation for '{request.ReferenceId}' already exists with status '{existing.Status}'.");
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

        if (!reservation.Commit())
        {
            return await MapToReservationResponseAsync(reservation, cancellationToken);
        }

        var committedAt = reservation.ConfirmedAt ?? DateTimeOffset.UtcNow;
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
                ledger.CreatedAt = committedAt;

                ledgers.Add(ledger);
            }
        }

        var response = await MapToReservationResponseAsync(reservation, cancellationToken);

        _dbContext.InventoryLedgers.AddRange(ledgers);
        // Xóa thẳng khỏi kho tạm để giải phóng bảng
        _dbContext.InventoryReservations.Remove(reservation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<InventoryReservationResponse> ReleaseReservationAsync(string referenceId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var reservation = await _dbContext.InventoryReservations
            .Include(r => r.ReservationItems)
            .FirstOrDefaultAsync(r => r.ReferenceId == referenceId, cancellationToken);

        // Kiểm tra xem đơn đã commit xuất kho chưa (dù còn hay đã xóa khỏi kho tạm)
        if (reservation == null || reservation.IsConfirmed)
        {
            var committed = await TryGetCommittedReservationAsync(referenceId, cancellationToken);
            if (committed != null)
            {
                throw new ReservationAlreadyCommittedException(
                    referenceId,
                    committed.ConfirmedAt ?? DateTimeOffset.UtcNow,
                    committed);
            }

            if (reservation == null)
            {
                return InventoryReservationResponse.AlreadyReleased(referenceId);
            }
        }

        if (!reservation.Release())
        {
            return await MapToReservationResponseAsync(reservation, cancellationToken);
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
            .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt <= now)
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

    /// <summary>
    /// Tái tạo thông tin reservation từ lịch sử sổ cái (InventoryLedgers) nếu đơn hàng đã được commit trước đó.
    /// Trả về <see cref="InventoryReservationResponse"/> nếu tìm thấy bản ghi commit trong sổ cái (hỗ trợ Idempotency); ngược lại trả về <c>null</c> nếu chưa từng commit.
    /// </summary>
    /// <param name="referenceId">Mã tham chiếu đơn hàng (Order/Reference ID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Thông tin reservation đã commit nếu tìm thấy trong sổ cái, hoặc <c>null</c> nếu không tồn tại.</returns>
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
            r.Status,
            r.ExpiresAt,
            r.ConfirmedAt,
            r.ReleasedAt,
            r.CreatedAt,
            itemResponses);
    }
}
