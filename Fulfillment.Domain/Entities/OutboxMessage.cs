namespace Fulfillment.Domain.Entities;

using System;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string AggregateType { get; set; }
    public required string AggregateId { get; set; }
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public required string Headers { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public string Status { get; set; } = "PENDING";
}
