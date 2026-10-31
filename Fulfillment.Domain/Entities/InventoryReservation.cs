namespace Fulfillment.Domain.Entities;

using System;
using System.Collections.Generic;

public class InventoryReservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ReferenceType { get; set; }
    public required string ReferenceId { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<InventoryReservationItem> ReservationItems { get; set; } = new List<InventoryReservationItem>();
    public ICollection<InventoryLedger> Ledgers { get; set; } = new List<InventoryLedger>();
}
