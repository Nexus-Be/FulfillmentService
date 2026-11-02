namespace Fulfillment.Domain.Entities;

using System;
using System.Collections.Generic;
using Fulfillment.Domain.Base;
using Fulfillment.Domain.Exceptions;

public class InventoryItem : BaseEntity<Guid>
{
    public required Guid SkuId { get; set; }
    public required Guid WarehouseId { get; set; }
    public int TotalQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int? AvailableQuantity { get; set; }

    // Navigation
    public Warehouse? Warehouse { get; set; }
    public ICollection<InventoryReservationItem> ReservationItems { get; set; } = new List<InventoryReservationItem>();
    public ICollection<InventoryLedger> Ledgers { get; set; } = new List<InventoryLedger>();

    public static InventoryItem Create(Guid skuId, Guid warehouseId, int initialQuantity)
    {
        return new InventoryItem
        {
            Id = Guid.NewGuid(),
            SkuId = skuId,
            WarehouseId = warehouseId,
            TotalQuantity = initialQuantity,
            ReservedQuantity = 0,
            AvailableQuantity = initialQuantity,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Adjust(int deltaQuantity)
    {
        int newTotal = TotalQuantity + deltaQuantity;

        if (newTotal < 0)
        {
            throw new BusinessRuleValidationException("Total quantity cannot be negative.");
        }

        if (newTotal < ReservedQuantity)
        {
            throw new BusinessRuleValidationException("Total quantity cannot be less than reserved quantity.");
        }

        TotalQuantity = newTotal;
        AvailableQuantity = TotalQuantity - ReservedQuantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Hold(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleValidationException("Quantity to hold must be greater than zero.");
        }

        var available = TotalQuantity - ReservedQuantity;
        if (available < quantity)
        {
            throw new InsufficientInventoryException(SkuId, quantity, available);
        }

        ReservedQuantity += quantity;
        AvailableQuantity = TotalQuantity - ReservedQuantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CommitHold(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleValidationException("Quantity to commit must be greater than zero.");
        }

        if (ReservedQuantity < quantity)
        {
            throw new BusinessRuleValidationException("Reserved quantity is insufficient to commit.");
        }

        TotalQuantity -= quantity;
        ReservedQuantity -= quantity;
        AvailableQuantity = TotalQuantity - ReservedQuantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReleaseHold(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleValidationException("Quantity to release must be greater than zero.");
        }

        if (ReservedQuantity < quantity)
        {
            throw new BusinessRuleValidationException("Reserved quantity is insufficient to release.");
        }

        ReservedQuantity -= quantity;
        AvailableQuantity = TotalQuantity - ReservedQuantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
