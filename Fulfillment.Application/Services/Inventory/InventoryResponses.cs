namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;
using Fulfillment.Domain.Entities;

public sealed record InventoryItemResponse(
    Guid Id,
    Guid SkuId,
    Guid WarehouseId,
    int TotalQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    DateTimeOffset UpdatedAt);

public sealed record AdjustInventoryResponse(
    Guid SkuId,
    int TotalQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    Guid AdjustmentLedgerId);

public sealed record InventoryLedgerResponse(
    Guid Id,
    Guid SkuId,
    string MovementType,
    int Quantity,
    Guid? ReservationId,
    string? ReferenceType,
    string? ReferenceId,
    string? Note,
    DateTimeOffset CreatedAt);

public sealed record InventoryReservationItemResponse(
    Guid SkuId,
    int Quantity);

public sealed record InventoryReservationResponse(
    Guid Id,
    string ReferenceId,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<InventoryReservationItemResponse> Items)
{
    public static InventoryReservationResponse FromCommittedLedgers(
        string referenceId,
        IReadOnlyList<InventoryLedger> ledgers,
        IReadOnlyDictionary<Guid, Guid> itemIdToSkuMap)
    {
        var earliestLedger = ledgers.MinBy(l => l.CreatedAt);
        var committedAt = earliestLedger?.CreatedAt ?? DateTimeOffset.UtcNow;
        var reservationId = earliestLedger?.ReservationId ?? Guid.Empty;

        var items = ledgers
            .Select(l => new InventoryReservationItemResponse(
                itemIdToSkuMap.TryGetValue(l.InventoryItemId, out var sku) ? sku : Guid.Empty,
                l.Quantity))
            .ToList();

        return new InventoryReservationResponse(
            reservationId,
            referenceId,
            committedAt,
            committedAt,
            items);
    }

    public static InventoryReservationResponse AlreadyReleased(string referenceId) =>
        new(
            Guid.Empty,
            referenceId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            Array.Empty<InventoryReservationItemResponse>());
}
