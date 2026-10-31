namespace Fulfillment.Application.Services.Inventory;

using System;

public record InitializeInventoryRequest(
    Guid SkuId,
    Guid? WarehouseId = null,
    int InitialQuantity = 0);
