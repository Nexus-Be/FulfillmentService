namespace Fulfillment.Domain.Entities;

using System;

public class InboxMessage
{
    public Guid MessageId { get; set; }
    public required string Consumer { get; set; }
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public string Status { get; set; } = "PENDING";
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}
