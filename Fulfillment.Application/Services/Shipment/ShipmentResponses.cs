namespace Fulfillment.Application.Services.Shipment;

using System;

public sealed record ShipmentResponse(
    Guid Id,
    Guid OrderId,
    Guid WarehouseId,
    Guid CarrierId,
    string? TrackingNumber,
    string Status,
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverAddress);

public sealed record CreateShipmentResponse(
    Guid ShipmentId,
    string TrackingNumber,
    string LabelUrl,
    string Status);
