namespace Fulfillment.Domain.Entities;

using System;
using System.Collections.Generic;
using Fulfillment.Domain.Base;
using Fulfillment.Domain.Constants;
using Fulfillment.Domain.Exceptions;

public class InventoryReservation : BaseEntity<Guid>
{
    public required string ReferenceId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    // Navigation
    public ICollection<InventoryReservationItem> ReservationItems { get; set; } = new List<InventoryReservationItem>();

    public static InventoryReservation Create(string referenceId, int holdMinutes = 30)
    {
        var effectiveHoldMinutes = holdMinutes > 0 ? holdMinutes : 30;
        var now = DateTimeOffset.UtcNow;

        return new InventoryReservation
        {
            // The key has a database default (gen_random_uuid), so EF does not assign it client-side and the in-memory Id stays Guid.Empty.
            // The reservation items copy this Id before SaveChanges, so it must be set here or they point to a parent that does not exist (FK 23503, HTTP 500).
            Id = Guid.NewGuid(),
            ReferenceId = referenceId,
            ExpiresAt = now.AddMinutes(effectiveHoldMinutes),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
