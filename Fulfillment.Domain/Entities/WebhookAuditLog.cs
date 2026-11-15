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
}

