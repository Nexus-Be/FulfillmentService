namespace Fulfillment.Application.Services.Inventory;

using System;
using Fulfillment.Application.Interfaces;
using Fulfillment.Domain.Entities;

public sealed partial class InventoryAppService : IInventoryAppService
{
    private readonly IFulfillmentDbContext _dbContext;

    public InventoryAppService(IFulfillmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    private static InventoryItemResponse MapToResponse(InventoryItem item) =>
        new InventoryItemResponse(
            item.Id,
            item.SkuId,
            item.WarehouseId,
            item.TotalQuantity,
            item.ReservedQuantity,
            item.AvailableQuantity ?? (item.TotalQuantity - item.ReservedQuantity),
            item.UpdatedAt);
}
