namespace Fulfillment.Application.Services.Shipment;

using System;

public sealed record ShipmentDto(
    Guid Id,
    Guid OrderId,
    Guid WarehouseId,
    Guid CarrierId,
    string? TrackingNumber,
    string Status,
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverAddress);

public sealed record CreateShipmentDto(
    Guid OrderId,
    Guid WarehouseId,
    Guid CarrierId,
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverAddress);
