namespace Fulfillment.Domain.Entities;

using System;
using System.Collections.Generic;
using Fulfillment.Domain.Constants;
using Fulfillment.Domain.Exceptions;

public class InventoryReservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ReferenceId { get; set; }
    public string Status { get; set; } = ReservationStatus.Active;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive => Status == ReservationStatus.Active;
    public bool IsConfirmed => Status == ReservationStatus.Confirmed;
    public bool IsReleased => Status == ReservationStatus.Released;
    public bool IsExpired => Status == ReservationStatus.Expired;

    // Navigation
    public ICollection<InventoryReservationItem> ReservationItems { get; set; } = new List<InventoryReservationItem>();

    public static InventoryReservation Create(string referenceId, int holdMinutes = 30)
    {
        var effectiveHoldMinutes = holdMinutes > 0 ? holdMinutes : 30;
        var now = DateTimeOffset.UtcNow;

        return new InventoryReservation
        {
            ReferenceId = referenceId,
            Status = ReservationStatus.Active,
            ExpiresAt = now.AddMinutes(effectiveHoldMinutes),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
