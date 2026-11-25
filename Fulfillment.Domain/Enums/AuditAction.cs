namespace Fulfillment.Domain.Enums;

public enum AuditAction
{
    ShipmentCreate = 0,
    ShipmentStatusChange = 1,
    ShipmentUpdate = 2,
    WarehouseCreate = 3,
    WarehouseUpdate = 4,
    WarehouseDelete = 5
}
