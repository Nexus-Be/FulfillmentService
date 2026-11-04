namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Fulfillment.Domain.Constants;
using Fulfillment.Domain.Entities;

public sealed record ReserveInventoryItemRequest(
    [Required(ErrorMessage = "SKU ID is required.")]
    Guid SkuId,

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    int Quantity);

public sealed record CreateReservationRequest(
    [Required(ErrorMessage = "Reference ID is required.")]
    string ReferenceId,

    [Required(ErrorMessage = "Items list cannot be null.")]
    IReadOnlyList<ReserveInventoryItemRequest> Items,

    int HoldMinutes = 30);

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

public sealed record ReleaseReservationItem(
    [Required] Guid SkuId,
    [Range(1, int.MaxValue)] int Quantity);

public sealed record ReleaseReservationItemsRequest(
    [Required] Guid SubOrderId,
    [Required] string Reason,
    [Required] IReadOnlyList<ReleaseReservationItem> Items);

public sealed record BatchReleaseReservationsRequest(
    [Required] IReadOnlyList<string> ReferenceIds,
    [Required] string Reason);

public sealed record ReleaseReservationResponse(
    string ReferenceId,
    string Status,
    int ReleasedCount);

public sealed record BatchReleaseReservationsResponse(
    int ReleasedCount,
    int FailedCount);
