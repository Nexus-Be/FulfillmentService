namespace Fulfillment.Domain.Enums;

public enum AuditAction
{
    WarehouseCreate = 0,
    WarehouseUpdate = 1,
    InventoryAdjust = 2,
    StockIn = 3,
    ReservationCreate = 4,
    ReservationConfirm = 5,
    ReservationRelease = 6,
    ShipmentCreate = 7,
    ShipmentDispatch = 8,
    ShipmentDeliver = 9
}
