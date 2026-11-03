namespace Fulfillment.Application.Services.Inventory;

using System;
using System.ComponentModel.DataAnnotations;
using Fulfillment.Domain.Enums;

public sealed record InventoryItemResponse(
    Guid Id,
    Guid SkuId,
    Guid WarehouseId,
    int TotalQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    DateTimeOffset UpdatedAt);

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

public sealed record AdjustInventoryResponse(
    Guid SkuId,
    int TotalQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    Guid AdjustmentLedgerId);
