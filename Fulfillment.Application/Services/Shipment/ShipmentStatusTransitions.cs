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

        // A failed delivery attempt can be retried by the carrier, so it may be followed by transit or delivery.
        if (current == ShipmentStatus.DeliveryFailed)
            return next is ShipmentStatus.InTransit or ShipmentStatus.Delivered or ShipmentStatus.Returned or ShipmentStatus.Cancelled;

        // Otherwise the enum order is the lifecycle order (ReadyToPick < Picking < Packed < HandedOver < InTransit < Delivered < ...).
        return (int)next > (int)current;
    }
}
