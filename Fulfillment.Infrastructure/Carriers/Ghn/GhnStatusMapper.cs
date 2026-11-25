namespace Fulfillment.Infrastructure.Carriers.Ghn;

using System;
using Fulfillment.Domain.Enums;

public static class GhnStatusMapper
{
    public static ShipmentStatus Map(string? rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            return ShipmentStatus.ReadyToPick;
        }

        return rawStatus.Trim().ToLowerInvariant() switch
        {
            "ready_to_pick" => ShipmentStatus.ReadyToPick,
            "picking" or "money_collect_picking" => ShipmentStatus.Picking,
            "storing" or "packed" => ShipmentStatus.Packed,
            "picked" => ShipmentStatus.HandedOver,
            "delivering" or "money_collect_delivering" or "transporting" or "sorting" => ShipmentStatus.InTransit,
            "delivered" => ShipmentStatus.Delivered,
            "delivery_fail" => ShipmentStatus.DeliveryFailed,
            // The parcel being on its way back is not the goods being back: only "returned" may cancel, refund and restock.
            "waiting_to_return" or "return" or "returning" or "return_transporting" or "return_sorting" => ShipmentStatus.Returning,
            "returned" or "return_to_origin" => ShipmentStatus.Returned,
            "cancel" => ShipmentStatus.Cancelled,
            "exception" or "lost" or "damage" or "scrap" => ShipmentStatus.DeliveryFailed,
            _ => ShipmentStatus.ReadyToPick
        };
    }
}

