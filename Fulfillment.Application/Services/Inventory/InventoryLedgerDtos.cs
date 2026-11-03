namespace Fulfillment.Application.Services.Inventory;

using System;
using System.ComponentModel.DataAnnotations;

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
