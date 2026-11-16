namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Fulfillment.Application.Common.Models;

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
    DateTimeOffset CreatedAt,
    Guid? WarehouseId = null,
    string? WarehouseName = null);

public sealed record InventoryLedgerFilterRequest(
    Guid? WarehouseId = null,
    Guid? SkuId = null,
    IReadOnlyList<Guid>? SkuIds = null,
    string? MovementType = null,
    string? ReferenceType = null,
    string? ReferenceId = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public sealed record InventoryLedgerSummaryResponse(
    int TotalInboundQuantity,
    int TotalOutboundQuantity,
    int NetChange,
    int TotalTransactions);

public sealed record InventoryLedgerPagedResponse(
    InventoryLedgerSummaryResponse Summary,
    PagedData<InventoryLedgerResponse> Items);
