namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Fulfillment.Domain.Enums;

public sealed record DeclareInventoryRequest(
    [Required(ErrorMessage = "SKU ID is required.")]
    Guid SkuId,

    [Required(ErrorMessage = "Warehouse ID is required.")]
    Guid WarehouseId,

    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 0.")]
    int Quantity,

    string? Note = null);

public sealed record AdjustInventoryRequest(
    [Required(ErrorMessage = "SKU ID is required.")]
    Guid SkuId,

    [Required(ErrorMessage = "Adjustment type is required.")]
    InventoryAdjustmentType Type,

    int Quantity,

    string? Note = null);

public sealed record RecordLedgerRequest(
    [Required(ErrorMessage = "SKU ID is required.")]
    Guid SkuId,

    [Required(ErrorMessage = "Movement type is required.")]
    string MovementType,

    int Quantity,

    Guid? ReservationId = null,
    string? ReferenceType = null,
    string? ReferenceId = null,
    string? Note = null);

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
