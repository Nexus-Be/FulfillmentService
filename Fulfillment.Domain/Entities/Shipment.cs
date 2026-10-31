namespace Fulfillment.Domain.Entities;

using System;
using System.Collections.Generic;

public class Shipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid OrderId { get; set; }
    public required Guid WarehouseId { get; set; }
    public required Guid CarrierId { get; set; }
    public string? ServiceType { get; set; }
    public decimal ShippingFee { get; set; }
    public string? TrackingNumber { get; set; }
    public string Status { get; set; } = "PENDING";
    public required string ReceiverName { get; set; }
    public required string ReceiverPhone { get; set; }
    public required string ReceiverAddress { get; set; }
    public DateTimeOffset? EstimatedDeliveryAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Warehouse? Warehouse { get; set; }
    public Carrier? Carrier { get; set; }
    public ICollection<ShipmentItem> Items { get; set; } = new List<ShipmentItem>();
    public ICollection<ShipmentEvent> Events { get; set; } = new List<ShipmentEvent>();
}
