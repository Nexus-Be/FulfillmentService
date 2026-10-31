namespace Fulfillment.Application.Services.Inventory;

using System;

public record InventoryItemResponse(
    Guid Id,
    Guid SkuId,
    Guid WarehouseId,
    int TotalQuantity,
    int AvailableQuantity,
    int ReservedQuantity,
    int UnavailableQuantity);
