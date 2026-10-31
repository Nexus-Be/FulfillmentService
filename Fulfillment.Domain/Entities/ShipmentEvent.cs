namespace Fulfillment.Domain.Entities;

using System;

public class ShipmentEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid ShipmentId { get; set; }
    public string? CarrierStatus { get; set; }
    public required string Status { get; set; }
    public string? Description { get; set; }
    public string? ExternalEventId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? RawPayload { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Shipment? Shipment { get; set; }
}
