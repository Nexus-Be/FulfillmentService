namespace Fulfillment.Domain.Entities;

using System;
using System.Collections.Generic;

public class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid SkuId { get; set; }
    public required Guid WarehouseId { get; set; }
    public int TotalQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int UnavailableQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Warehouse? Warehouse { get; set; }
    public ICollection<InventoryReservationItem> ReservationItems { get; set; } = new List<InventoryReservationItem>();
    public ICollection<InventoryLedger> Ledgers { get; set; } = new List<InventoryLedger>();
}
