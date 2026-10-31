namespace Fulfillment.Domain.Entities;

using System;

public class IdempotencyKey
{
    public required string Key { get; set; }
    public required string Scope { get; set; }
    public required string RequestHash { get; set; }
    public int? ResponseStatus { get; set; }
    public string? ResponseBody { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
}
