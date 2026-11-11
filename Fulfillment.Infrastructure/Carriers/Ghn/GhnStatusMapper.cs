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
            "picking" => ShipmentStatus.Picking,
            "storing" or "packed" => ShipmentStatus.Packed,
            "picked" => ShipmentStatus.HandedOver,
            "delivering" or "money_collect_delivering" => ShipmentStatus.InTransit,
            "delivered" => ShipmentStatus.Delivered,
            "delivery_fail" => ShipmentStatus.DeliveryFailed,
            "return" or "returning" or "returned" or "return_to_origin" => ShipmentStatus.Returned,
            "cancel" => ShipmentStatus.Cancelled,
            _ => ShipmentStatus.ReadyToPick
        };
    }
}

