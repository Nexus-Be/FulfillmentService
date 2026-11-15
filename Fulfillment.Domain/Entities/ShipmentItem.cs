namespace Fulfillment.Domain.Entities;

using System;
using Fulfillment.Domain.Base;

public class ShipmentItem : BaseEntity<Guid>
{
    public required Guid ShipmentId { get; set; }
    public required Guid SkuId { get; set; }
    public required int Quantity { get; set; }
    public string Name { get; set; } = "Product";
    public decimal Price { get; set; } = 0;
    public int Weight { get; set; } = 100;
    public int Length { get; set; } = 10;
    public int Width { get; set; } = 10;
    public int Height { get; set; } = 10;

    // Navigation
    public Shipment? Shipment { get; set; }
}
