namespace Fulfillment.Domain.Enums;

public enum ShipmentStatus
{
    ReadyToPick = 0,
    Picking = 1,
    Packed = 2,
    HandedOver = 3,
    InTransit = 4,
    Delivered = 5,
    DeliveryFailed = 6,
    Returned = 7,
    Cancelled = 8
}
