namespace Fulfillment.Domain.Entities;

using System;

public class InventoryReservationItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid ReservationId { get; set; }
    public required Guid InventoryItemId { get; set; }
    public int Quantity { get; set; }

    // Navigation
    public InventoryReservation? Reservation { get; set; }
    public InventoryItem? InventoryItem { get; set; }
}
