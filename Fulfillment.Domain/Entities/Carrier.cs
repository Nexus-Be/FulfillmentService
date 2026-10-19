using Fulfillment.Domain.Base;

namespace Fulfillment.Domain.Entities;

public class Carrier : BaseEntity<Guid>
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int Priority { get; set; }
    public bool IsDisabled { get; set; }

    // Navigation
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
