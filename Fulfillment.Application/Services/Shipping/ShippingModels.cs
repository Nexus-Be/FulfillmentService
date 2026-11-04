namespace Fulfillment.Application.Services.Shipping;

using System;
using System.Collections.Generic;
using Fulfillment.Domain.Enums;

public sealed record ShippingAddress(
    string FullName,
    string Phone,
    string AddressLine,
    string? WardCode = null,
    string? WardName = null,
    int? DistrictId = null,
    string? DistrictName = null,
    string? ProvinceName = null);

public sealed record ShippingParcelItem(
    string SkuId,
    string? Name,
    int Quantity,
    int WeightInGrams,
    int? LengthCm = null,
    int? WidthCm = null,
    int? HeightCm = null);

public sealed record CarrierFeeCalculationRequest(
    ShippingAddress FromAddress,
    ShippingAddress ToAddress,
    int TotalWeightInGrams,
    int? LengthCm = null,
    int? WidthCm = null,
    int? HeightCm = null,
    decimal CodAmount = 0,
    IReadOnlyList<ShippingParcelItem>? Items = null);

public sealed record CarrierFeeCalculationResult(
    decimal TotalFee,
    DateTimeOffset? ExpectedDeliveryTime = null,
    string? ServiceCode = null);

public sealed record CarrierCreateShipmentRequest(
    string InternalOrderCode,
    ShippingAddress FromAddress,
    ShippingAddress ToAddress,
    int TotalWeightInGrams,
    int? LengthCm = null,
    int? WidthCm = null,
    int? HeightCm = null,
    decimal CodAmount = 0,
    string? Note = null,
    IReadOnlyList<ShippingParcelItem>? Items = null);

public sealed record CarrierCreateShipmentResult(
    string TrackingCode,
    decimal ShippingFee,
    DateTimeOffset? ExpectedDeliveryTime,
    ShipmentStatus InitialStatus,
    string? LabelUrl = null);

public sealed record CarrierStatusResult(
    string TrackingCode,
    ShipmentStatus Status,
    string RawStatus,
    string? Description = null,
    DateTimeOffset? UpdatedAt = null);

public sealed record CarrierCancelResult(
    string TrackingCode,
    bool Success,
    string? Message = null);

