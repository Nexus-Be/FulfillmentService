using Fulfillment.Domain.Base;

namespace Fulfillment.Domain.Entities;

public class InventoryItem : BaseEntity<Guid>
{
    public required Guid SkuId { get; set; }
    public required Guid WarehouseId { get; set; }
    public int TotalQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int UnavailableQuantity { get; set; }
    public int AvailableQuantity { get; set; }

    // Navigation
    public Warehouse? Warehouse { get; set; }
    public ICollection<InventoryReservationItem> ReservationItems { get; set; } = new List<InventoryReservationItem>();
    public ICollection<InventoryLedger> Ledgers { get; set; } = new List<InventoryLedger>();
}
