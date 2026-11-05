namespace Fulfillment.Domain.Entities;

using System;
using Fulfillment.Domain.Base;

public class ShipmentItem : BaseEntity<Guid>
{
    public required Guid ShipmentId { get; set; }
    public required Guid SkuId { get; set; }
    public required int Quantity { get; set; }

    // Navigation
    public Shipment? Shipment { get; set; }
}
