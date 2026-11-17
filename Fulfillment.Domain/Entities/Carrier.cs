namespace Fulfillment.Domain.Entities;

using System;
using System.Collections.Generic;
using Fulfillment.Domain.Base;

public class Carrier : BaseEntity<Guid>
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
