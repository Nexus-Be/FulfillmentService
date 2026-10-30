namespace Fulfillment.Application.Services.Shipment;

using System;

public sealed record CreateShipmentRequest(
    Guid OrderId,
    Guid WarehouseId,
    Guid CarrierId,
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverAddress);

public sealed record UpdateShipmentStatusRequest(
    string TrackingNumber,
    string Status,
    string? Note = null);
