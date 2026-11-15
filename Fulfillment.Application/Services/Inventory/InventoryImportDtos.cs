namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;

public sealed record InventoryImportPreviewItem(
    int RowNumber,
    Guid? SkuId,
    string SkuCode,
    int Quantity,
    string? Note,
    bool IsValid,
    string? ErrorMessage);

public sealed record InventoryImportUnmatchedItem(
    int RowNumber,
    string RawSkuCode,
    int Quantity,
    string? Note,
    string Reason);

public sealed record InventoryImportPreviewResponse(
    Guid WarehouseId,
    int TotalRowsRead,
    IReadOnlyList<InventoryImportPreviewItem> ReadyToImportItems,
    IReadOnlyList<InventoryImportUnmatchedItem> UnmatchedNewItems);

