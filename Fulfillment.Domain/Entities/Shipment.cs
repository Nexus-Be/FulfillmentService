using Fulfillment.Domain.Base;
using Fulfillment.Domain.Enums;

namespace Fulfillment.Domain.Entities;

public class Shipment : AuditableEntity<Guid>
{
    public required Guid OrderId { get; set; }
    public required Guid WarehouseId { get; set; }
    public required Guid CarrierId { get; set; }
    public ShippingServiceType ServiceType { get; set; } = ShippingServiceType.Standard;
    public decimal ShippingFee { get; set; }
    public string? TrackingNumber { get; set; }
    public required ShipmentStatus Status { get; set; } = ShipmentStatus.ReadyToPick;
    public required string ReceiverName { get; set; }
    public required string ReceiverPhone { get; set; }
    public required string ReceiverAddress { get; set; }
    public DateTimeOffset? EstimatedDeliveryAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }

    // Navigation
    public Warehouse? Warehouse { get; set; }
    public Carrier? Carrier { get; set; }
    public ICollection<ShipmentItem> Items { get; set; } = new List<ShipmentItem>();
    public ICollection<ShipmentEvent> Events { get; set; } = new List<ShipmentEvent>();
}
