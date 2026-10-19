namespace Fulfillment.Domain.Enums;

public enum ShipmentEventStatus
{
    Created = 0,
    PickedUp = 1,
    InTransit = 2,
    OutForDelivery = 3,
    Delivered = 4,
    DeliveryFailed = 5,
    Returned = 6
}
