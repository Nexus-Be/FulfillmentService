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

    public static InventoryLedger Create(
        Guid inventoryItemId,
        string movementType,
        int quantity,
        string? referenceType = "MANUAL",
        string? referenceId = null,
        string? note = null,
        Guid? reservationId = null)
    {
        return new InventoryLedger
        {
            InventoryItemId = inventoryItemId,
            MovementType = movementType,
            Quantity = quantity,
            ReservationId = reservationId,
            ReferenceType = referenceType ?? "MANUAL",
            ReferenceId = referenceId,
            Note = note,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
