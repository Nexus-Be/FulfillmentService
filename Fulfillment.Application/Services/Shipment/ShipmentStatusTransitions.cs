namespace Fulfillment.Application.Services.Shipment;

using Fulfillment.Domain.Enums;

/// <summary>
/// Carrier webhooks can arrive late, twice or out of order. A shipment may only move forward, so a stale "in transit"
/// cannot undo "delivered", and an unknown carrier status (which maps to ReadyToPick) cannot reset a shipment.
/// </summary>
public static class ShipmentStatusTransitions
{
    /// <summary>True when <paramref name="next"/> is a real step forward from <paramref name="current"/>.</summary>
    public static bool IsForward(ShipmentStatus current, ShipmentStatus next)
    {
        if (next == current)
            return false;

        // Returned and Cancelled end the shipment; nothing follows them.
        if (current is ShipmentStatus.Returned or ShipmentStatus.Cancelled)
            return false;

        // Delivered is final for the delivery itself: a late "delivery failed" from the carrier must neither undo it nor count as an attempt.
        // The only thing that can still follow is the parcel coming back as Returned.
        if (current == ShipmentStatus.Delivered)
            return next == ShipmentStatus.Returned;

        // Returning is only reachable once the parcel is with the carrier, and it can only end as Returned (or Cancelled).
        if (next == ShipmentStatus.Returning)
            return current is ShipmentStatus.HandedOver or ShipmentStatus.InTransit or ShipmentStatus.DeliveryFailed;

        if (current == ShipmentStatus.Returning)
            return next is ShipmentStatus.Returned or ShipmentStatus.Cancelled;

        // A failed delivery attempt can be retried by the carrier, so it may be followed by transit or delivery.
        if (current == ShipmentStatus.DeliveryFailed)
            return next is ShipmentStatus.InTransit or ShipmentStatus.Delivered or ShipmentStatus.Returned or ShipmentStatus.Cancelled;

        // Otherwise the enum order is the lifecycle order (ReadyToPick < Picking < Packed < HandedOver < InTransit < Delivered < ...).
        return (int)next > (int)current;
    }

    /// <summary>
    /// A second failed delivery attempt reports the same status as the first, so it is not a step "forward". The caller must have established
    /// that the event is new (different carrier event time) before treating it as another attempt.
    /// </summary>
    public static bool IsRepeatedFailedAttempt(ShipmentStatus current, ShipmentStatus next) =>
        current == ShipmentStatus.DeliveryFailed && next == ShipmentStatus.DeliveryFailed;
}
