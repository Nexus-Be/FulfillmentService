using Fulfillment.Domain.Base;

namespace Fulfillment.Domain.Entities;

public class ShipmentItem : BaseEntity<Guid>
{
    public required Guid ShipmentId { get; set; }
    public required Guid SkuId { get; set; }
    public required int Quantity { get; set; }

    // Navigation
    public Shipment? Shipment { get; set; }
}
