namespace Fulfillment.Application.Services.Shipment;

using System;
using System.ComponentModel.DataAnnotations;

public sealed record CreateShipmentRequest(
    [Required] Guid OrderId,
    [Required] Guid SubOrderId,
    [Required] Guid SellerId,
    [Required] string RecipientName,
    [Required] string RecipientPhone,
    [Required] string ShippingAddress,
    int? ToDistrictId = null,
    string? ToWardCode = null,
    int WeightInGrams = 500);

public sealed record UpdateShipmentStatusRequest(
    string TrackingNumber,
    string Status,
    string? Note = null);
