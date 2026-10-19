using Fulfillment.Domain.Base;

namespace Fulfillment.Domain.Entities;

public class InventoryReservationItem : BaseEntity<Guid>
{
    public required Guid ReservationId { get; set; }
    public required Guid InventoryItemId { get; set; }
    public required int Quantity { get; set; }

    // Navigation
    public InventoryReservation? Reservation { get; set; }
    public InventoryItem? InventoryItem { get; set; }
}
