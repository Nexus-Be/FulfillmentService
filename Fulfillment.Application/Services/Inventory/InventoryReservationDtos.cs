namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public sealed record ReserveInventoryItemRequest(
    [Required(ErrorMessage = "SKU ID is required.")]
    Guid SkuId,

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    int Quantity);

public sealed record CreateReservationRequest(
    [Required(ErrorMessage = "Reference type is required.")]
    string ReferenceType,

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
    string ReferenceType,
    string ReferenceId,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? ReleasedAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<InventoryReservationItemResponse> Items);
