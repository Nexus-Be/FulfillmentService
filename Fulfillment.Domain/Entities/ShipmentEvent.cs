namespace Fulfillment.Domain.Entities;

using System;
using Fulfillment.Domain.Base;
using Fulfillment.Domain.Enums;

public class ShipmentEvent : BaseEntity<Guid>
{
    public required Guid ShipmentId { get; set; }
    public string? CarrierStatus { get; set; }
    public required ShipmentEventStatus Status { get; set; }
    public string? Description { get; set; }
    public string? ExternalEventId { get; set; }
    public required DateTimeOffset OccurredAt { get; set; }
    public string? RawPayload { get; set; }
    public string? ReasonCode { get; set; }
    public string? Reason { get; set; }
    public string? ShipperName { get; set; }
    public string? ShipperPhone { get; set; }
    public string? PodUrl { get; set; }
    public string? WarehouseName { get; set; }

    // Navigation
    public Shipment? Shipment { get; set; }
}
