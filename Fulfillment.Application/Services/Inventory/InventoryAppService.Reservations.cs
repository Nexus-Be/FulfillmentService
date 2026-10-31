namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Exceptions;

public sealed partial class InventoryAppService
{
    public async Task<InventoryReservationResponse> CreateReservationAsync(CreateReservationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new BusinessRuleValidationException("Reservation must contain at least one item.");
        }

        var existing = await _dbContext.InventoryReservations
            .Include(r => r.ReservationItems)
            .FirstOrDefaultAsync(r => r.ReferenceType == request.ReferenceType && r.ReferenceId == request.ReferenceId, cancellationToken);

        if (existing != null)
        {
            if (existing.Status == "ACTIVE")
            {
                return await MapToReservationResponseAsync(existing, cancellationToken);
            }
            throw new BusinessRuleValidationException($"Reservation for {request.ReferenceType} '{request.ReferenceId}' already exists with status '{existing.Status}'.");
        }

        var aggregatedRequests = request.Items
            .GroupBy(x => x.SkuId)
            .Select(g => new { SkuId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .OrderBy(x => x.SkuId)
            .ToList();

        var skuIds = aggregatedRequests.Select(x => x.SkuId).ToList();

        var items = await _dbContext.InventoryItems
            .Where(i => skuIds.Contains(i.SkuId))
            .ToListAsync(cancellationToken);

        var itemMap = items.ToDictionary(i => i.SkuId);

        foreach (var req in aggregatedRequests)
        {
            if (!itemMap.TryGetValue(req.SkuId, out var item))
            {
                throw new EntityNotFoundException("InventoryItem for SKU", req.SkuId);
            }

            var available = item.TotalQuantity - item.ReservedQuantity - item.UnavailableQuantity;
            if (available < req.Quantity)
            {
                throw new InsufficientInventoryException(req.SkuId, req.Quantity, available);
            }
        }

        var holdMinutes = request.HoldMinutes > 0 ? request.HoldMinutes : 30;
        var reservation = new InventoryReservation
        {
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            Status = "ACTIVE",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(holdMinutes),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.InventoryReservations.Add(reservation);

        var reservationItems = new List<InventoryReservationItem>();
        var ledgers = new List<InventoryLedger>();

        foreach (var req in aggregatedRequests)
        {
            var item = itemMap[req.SkuId];
            item.ReservedQuantity += req.Quantity;
            item.AvailableQuantity = item.TotalQuantity - item.ReservedQuantity - item.UnavailableQuantity;
            item.UpdatedAt = DateTimeOffset.UtcNow;

            var resItem = new InventoryReservationItem
            {
                ReservationId = reservation.Id,
                InventoryItemId = item.Id,
                Quantity = req.Quantity
            };
            reservationItems.Add(resItem);

            var ledger = new InventoryLedger
            {
                InventoryItemId = item.Id,
                MovementType = "RESERVE",
                Quantity = req.Quantity,
                ReservationId = reservation.Id,
                ReferenceType = request.ReferenceType,
                ReferenceId = request.ReferenceId,
                Note = $"Reserved {req.Quantity} for {request.ReferenceType} {request.ReferenceId}",
                CreatedAt = DateTimeOffset.UtcNow
            };
            ledgers.Add(ledger);
        }

        _dbContext.InventoryReservationItems.AddRange(reservationItems);
        _dbContext.InventoryLedgers.AddRange(ledgers);

        await _dbContext.SaveChangesAsync(cancellationToken);

        reservation.ReservationItems = reservationItems;
        return await MapToReservationResponseAsync(reservation, cancellationToken);
    }

    public async Task<InventoryReservationResponse> CommitReservationAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default)
    {
        var reservation = await _dbContext.InventoryReservations
            .Include(r => r.ReservationItems)
            .FirstOrDefaultAsync(r => r.ReferenceType == referenceType && r.ReferenceId == referenceId, cancellationToken)
            ?? throw new EntityNotFoundException($"InventoryReservation for {referenceType}", referenceId);

        if (reservation.Status == "CONFIRMED")
        {
            return await MapToReservationResponseAsync(reservation, cancellationToken);
        }

        if (reservation.Status != "ACTIVE")
        {
            throw new BusinessRuleValidationException($"Cannot commit reservation with status '{reservation.Status}'.");
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
                item.TotalQuantity -= resItem.Quantity;
                item.ReservedQuantity -= resItem.Quantity;
                item.AvailableQuantity = item.TotalQuantity - item.ReservedQuantity - item.UnavailableQuantity;
                item.UpdatedAt = DateTimeOffset.UtcNow;

                ledgers.Add(new InventoryLedger
                {
                    InventoryItemId = item.Id,
                    MovementType = "COMMIT",
                    Quantity = resItem.Quantity,
                    ReservationId = reservation.Id,
                    ReferenceType = referenceType,
                    ReferenceId = referenceId,
                    Note = $"Committed {resItem.Quantity} for {referenceType} {referenceId}",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        reservation.Status = "CONFIRMED";
        reservation.ConfirmedAt = DateTimeOffset.UtcNow;
        reservation.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.InventoryLedgers.AddRange(ledgers);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapToReservationResponseAsync(reservation, cancellationToken);
    }

    public async Task<InventoryReservationResponse> ReleaseReservationAsync(string referenceType, string referenceId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var reservation = await _dbContext.InventoryReservations
            .Include(r => r.ReservationItems)
            .FirstOrDefaultAsync(r => r.ReferenceType == referenceType && r.ReferenceId == referenceId, cancellationToken)
            ?? throw new EntityNotFoundException($"InventoryReservation for {referenceType}", referenceId);

        if (reservation.Status == "RELEASED")
        {
            return await MapToReservationResponseAsync(reservation, cancellationToken);
        }

        if (reservation.Status != "ACTIVE")
        {
            throw new BusinessRuleValidationException($"Cannot release reservation with status '{reservation.Status}'.");
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
                item.ReservedQuantity -= resItem.Quantity;
                item.AvailableQuantity = item.TotalQuantity - item.ReservedQuantity - item.UnavailableQuantity;
                item.UpdatedAt = DateTimeOffset.UtcNow;

                ledgers.Add(new InventoryLedger
                {
                    InventoryItemId = item.Id,
                    MovementType = "RELEASE",
                    Quantity = resItem.Quantity,
                    ReservationId = reservation.Id,
                    ReferenceType = referenceType,
                    ReferenceId = referenceId,
                    Note = reason ?? $"Released {resItem.Quantity} for {referenceType} {referenceId}",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        reservation.Status = "RELEASED";
        reservation.ReleasedAt = DateTimeOffset.UtcNow;
        reservation.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.InventoryLedgers.AddRange(ledgers);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapToReservationResponseAsync(reservation, cancellationToken);
    }

    public async Task<int> AutoExpireReservationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredReservations = await _dbContext.InventoryReservations
            .Include(r => r.ReservationItems)
            .Where(r => r.Status == "ACTIVE" && r.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expiredReservations.Count == 0)
        {
            return 0;
        }

        var allItemIds = expiredReservations
            .SelectMany(r => r.ReservationItems.Select(ri => ri.InventoryItemId))
            .Distinct()
            .ToList();

        var items = await _dbContext.InventoryItems
            .Where(i => allItemIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        var itemMap = items.ToDictionary(i => i.Id);
        var ledgers = new List<InventoryLedger>();

        foreach (var reservation in expiredReservations)
        {
            foreach (var resItem in reservation.ReservationItems)
            {
                if (itemMap.TryGetValue(resItem.InventoryItemId, out var item))
                {
                    item.ReservedQuantity -= resItem.Quantity;
                    item.AvailableQuantity = item.TotalQuantity - item.ReservedQuantity - item.UnavailableQuantity;
                    item.UpdatedAt = DateTimeOffset.UtcNow;

                    ledgers.Add(new InventoryLedger
                    {
                        InventoryItemId = item.Id,
                        MovementType = "RELEASE",
                        Quantity = resItem.Quantity,
                        ReservationId = reservation.Id,
                        ReferenceType = reservation.ReferenceType,
                        ReferenceId = reservation.ReferenceId,
                        Note = "Auto-expired hold released back to available stock",
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
            }

            reservation.Status = "EXPIRED";
            reservation.ReleasedAt = DateTimeOffset.UtcNow;
            reservation.UpdatedAt = DateTimeOffset.UtcNow;
        }

        _dbContext.InventoryLedgers.AddRange(ledgers);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return expiredReservations.Count;
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
            r.ReferenceType,
            r.ReferenceId,
            r.Status,
            r.ExpiresAt,
            r.ConfirmedAt,
            r.ReleasedAt,
            r.CreatedAt,
            itemResponses);
    }
}
