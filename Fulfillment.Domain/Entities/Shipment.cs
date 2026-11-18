namespace Fulfillment.Domain.Entities;

using System;
using System.Collections.Generic;
using Fulfillment.Domain.Base;
using Fulfillment.Domain.Enums;

public class Shipment : AuditableEntity<Guid>
{
    public required Guid OrderId { get; set; }
    public Guid? BuyerId { get; set; }
    public required Guid WarehouseId { get; set; }
    public required Guid CarrierId { get; set; }
    public ShippingServiceType ServiceType { get; set; } = ShippingServiceType.Standard;
    public decimal ShippingFee { get; set; }
    public string? TrackingNumber { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.ReadyToPick;
    public required string ReceiverName { get; set; }
    public required string ReceiverPhone { get; set; }
    public required string ReceiverAddress { get; set; }
    public string? ToProvinceName { get; set; }
    public int? ToDistrictId { get; set; }
    public string? ToWardCode { get; set; }
    public int Weight { get; set; } = 500;
    public int Length { get; set; } = 20;
    public int Width { get; set; } = 15;
    public int Height { get; set; } = 10;
    public int? ConvertedWeight { get; set; }
    public int CodAmount { get; set; } = 0;
    public int InsuranceValue { get; set; } = 0;
    public int PaymentTypeId { get; set; } = 1;
    public GHNRequiredNote RequiredNote { get; set; } = GHNRequiredNote.CHOXEMHANGKHONGTHU;
    public string? Note { get; set; }
    public string? ClientOrderCode { get; set; }
    public string? GhnOrderCode { get; set; }
    public string? PrintToken { get; set; }
    public DateTimeOffset? EstimatedDeliveryAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }

    // Navigation
    public Warehouse? Warehouse { get; set; }
    public Carrier? Carrier { get; set; }
    public ICollection<ShipmentItem> Items { get; set; } = new List<ShipmentItem>();
    public ICollection<ShipmentEvent> Events { get; set; } = new List<ShipmentEvent>();
}
