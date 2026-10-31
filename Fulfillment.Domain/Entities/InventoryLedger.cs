namespace Fulfillment.Domain.Entities;

using System;

public class InventoryLedger
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid InventoryItemId { get; set; }
    public required string MovementType { get; set; }
    public int Quantity { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public InventoryItem? InventoryItem { get; set; }
    public InventoryReservation? Reservation { get; set; }
}
