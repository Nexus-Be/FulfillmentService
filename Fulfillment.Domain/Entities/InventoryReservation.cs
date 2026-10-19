using Fulfillment.Domain.Base;
using Fulfillment.Domain.Enums;

namespace Fulfillment.Domain.Entities;

public class InventoryReservation : BaseEntity<Guid>
{
    public required InventoryReservationReferenceType ReferenceType { get; set; }
    public required string ReferenceId { get; set; }
    public required InventoryReservationStatus Status { get; set; } = InventoryReservationStatus.Pending;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }

    // Navigation
    public ICollection<InventoryReservationItem> ReservationItems { get; set; } = new List<InventoryReservationItem>();
    public ICollection<InventoryLedger> Ledgers { get; set; } = new List<InventoryLedger>();
}
