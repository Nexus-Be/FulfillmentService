using Fulfillment.Domain.Base;
using Fulfillment.Domain.Enums;

namespace Fulfillment.Domain.Entities;

public class InventoryLedger : BaseEntity<Guid>
{
    public required Guid InventoryItemId { get; set; }
    public required InventoryMovementType MovementType { get; set; }
    public required int Quantity { get; set; }
    public Guid? ReservationId { get; set; }
    public InventoryLedgerReferenceType? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string? Note { get; set; }

    // Navigation
    public InventoryItem? InventoryItem { get; set; }
    public InventoryReservation? Reservation { get; set; }
}
