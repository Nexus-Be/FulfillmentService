namespace Fulfillment.Domain.Entities;

using System;
using System.Collections.Generic;

public class Carrier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
