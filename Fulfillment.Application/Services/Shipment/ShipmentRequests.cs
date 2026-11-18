namespace Fulfillment.Application.Services.Shipment;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public sealed record CreateShipmentItemRequest(
    [Required] Guid SkuId,
    [Range(1, int.MaxValue)] int Quantity,
    string? Name = null,
    decimal Price = 0,
    int? WeightInGrams = null,
    int? LengthCm = null,
    int? WidthCm = null,
    int? HeightCm = null);

public sealed record CreateShipmentRequest(
    [Required] Guid OrderId,
    [Required] Guid SubOrderId,
    [Required] Guid SellerId,
    [Required] string RecipientName,
    [Required] string RecipientPhone,
    [Required] string ShippingAddress,
    Guid? BuyerId = null,
    string? ToProvinceName = null,
    int? ToDistrictId = null,
    string? ToWardCode = null,
    int WeightInGrams = 500,
    int LengthCm = 20,
    int WidthCm = 15,
    int HeightCm = 10,
    int CodAmount = 0,
    int InsuranceValue = 0,
    string? RequiredNote = "CHOXEMHANGKHONGTHU",
    string? Note = null,
    IReadOnlyList<CreateShipmentItemRequest>? Items = null);

public sealed record UpdateShipmentStatusRequest(
    string TrackingNumber,
    string Status,
    string? Note = null);

public sealed record SimulateShipperStatusRequest(
    string? TrackingNumber = null,
    [Required] string Status = "",
    string? Description = null,
    string? ShipperName = "Nguyễn Văn Shipper",
    string? ShipperPhone = "0901234567",
    string? ReasonCode = null,
    string? Reason = null,
    string? PodUrl = null,
    bool Force = false);

public sealed record CancelShipmentRequest(
    string? Reason = null);

public sealed record UpdateRecipientAddressRequest(
    [Required] string RecipientName,
    [Required] string RecipientPhone,
    [Required] string ShippingAddress,
    string? ToProvinceName = null,
    int? ToDistrictId = null,
    string? ToWardCode = null);
