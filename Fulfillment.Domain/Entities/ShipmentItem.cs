namespace Fulfillment.Domain.Entities;

using System;

public class ShipmentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid ShipmentId { get; set; }
    public required Guid SkuId { get; set; }
    public int Quantity { get; set; }

    // Navigation
    public Shipment? Shipment { get; set; }
}
