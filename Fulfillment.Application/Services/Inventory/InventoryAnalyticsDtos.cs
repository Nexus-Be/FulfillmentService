namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Fulfillment.Application.Common.Models;

public sealed record UpdateStockThresholdRequest(
    [Range(0, 1000000, ErrorMessage = "Min stock threshold must be between 0 and 1,000,000.")]
    int MinStockThreshold,

    bool? IsLowStockAlertEnabled = null);

public sealed record LowStockAlertItemResponse(
    Guid SkuId,
    Guid WarehouseId,
    int AvailableQuantity,
    int TotalQuantity,
    int ReservedQuantity,
    int MinStockThreshold,
    DateTimeOffset LastMovementAt);

public sealed record DeadStockAlertItemResponse(
    Guid SkuId,
    Guid WarehouseId,
    int AvailableQuantity,
    int TotalQuantity,
    DateTimeOffset LastMovementAt,
    int DaysWithoutMovement);

public sealed record InventoryAnalyticsFilterRequest(
    Guid? WarehouseId = null,
    IReadOnlyList<Guid>? SkuIds = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    string? SortBy = null,
    string? SortDirection = "desc",
    int Page = 1,
    int PageSize = 20);

public sealed record InventoryAnalyticsSummaryResponse(
    int TotalItems,
    int TotalStockQuantity,
    int TotalAvailableQuantity,
    int TotalReservedQuantity,
    int TotalSoldQuantity,
    int LowStockCount,
    int DeadStockCount);

public sealed record InventoryAnalyticsResponse(
    InventoryAnalyticsSummaryResponse Summary,
    PagedData<InventoryItemResponse> Items);

