namespace Fulfillment.Domain.Entities;

using System;
using Fulfillment.Domain.Base;

public class InventoryReservationItem : BaseEntity<Guid>
{
    public required Guid ReservationId { get; set; }
    public required Guid InventoryItemId { get; set; }
    public int Quantity { get; set; }

    // Navigation
    public InventoryReservation? Reservation { get; set; }
    public InventoryItem? InventoryItem { get; set; }
}
