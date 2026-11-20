namespace Fulfillment.Domain.Entities;

using System;
using Fulfillment.Domain.Base;

public class WebhookAuditLog : BaseEntity<Guid>
{
    public required string OrderCode { get; set; }
    public string? ClientOrderCode { get; set; }
    public string? EventType { get; set; }
    public required string Status { get; set; }
    public string? RawJson { get; set; }
    public bool IsProcessed { get; set; } = false;
    public string? ProcessMessage { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>The carrier event time, normalised to UTC (yyyy-MM-ddTHH:mm:ss.fffZ). Two webhooks with the same status but a different time are different events.</summary>
    public string? EventTimeKey { get; set; }

    // Outbox for the delivery update owed to CommerceService. Set when the webhook moved the shipment to a status Commerce must hear about,
    // cleared (CommerceNotifiedAt set) only once Commerce has acknowledged it, so an outage cannot lose the update.
    public Guid? NotifyOrderId { get; set; }
    public string? NotifyStatus { get; set; }
    public string? NotifyTrackingNumber { get; set; }
    public string? NotifyDescription { get; set; }
    public DateTimeOffset? CommerceNotifiedAt { get; set; }
    public int CommerceNotifyAttempts { get; set; }
    public DateTimeOffset? CommerceNotifyNextRetryAt { get; set; }
}

