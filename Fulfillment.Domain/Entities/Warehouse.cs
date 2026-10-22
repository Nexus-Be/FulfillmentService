using Fulfillment.Domain.Base;
using Fulfillment.Domain.Enums;

namespace Fulfillment.Domain.Entities;

public class Warehouse : AuditableEntity<Guid>
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public WarehouseType Type { get; set; } = WarehouseType.PlatformWarehouse;
    public Guid? SellerId { get; set; }
    public string? Address { get; set; }
    public bool IsDisabled { get; set; }

    // Navigation
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
