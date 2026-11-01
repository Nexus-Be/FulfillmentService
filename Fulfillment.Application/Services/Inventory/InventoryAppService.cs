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

    private static InventoryLedger CreateLedger(
        Guid inventoryItemId,
        string movementType,
        int quantity,
        string? referenceType = "MANUAL",
        string? referenceId = null,
        string? note = null,
        Guid? reservationId = null)
    {
        return new InventoryLedger
        {
            InventoryItemId = inventoryItemId,
            MovementType = movementType,
            Quantity = quantity,
            ReservationId = reservationId,
            ReferenceType = referenceType ?? "MANUAL",
            ReferenceId = referenceId,
            Note = note,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static void RefreshAvailableQuantity(InventoryItem item)
    {
        item.AvailableQuantity = item.TotalQuantity - item.ReservedQuantity - item.UnavailableQuantity;
        item.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static InventoryItemResponse MapToResponse(InventoryItem item) =>
        new InventoryItemResponse(
            item.Id,
            item.SkuId,
            item.WarehouseId,
            item.TotalQuantity,
            item.ReservedQuantity,
            item.UnavailableQuantity,
            item.AvailableQuantity ?? (item.TotalQuantity - item.ReservedQuantity - item.UnavailableQuantity),
            item.UpdatedAt);
}
