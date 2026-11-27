namespace Fulfillment.Application.Services.Shipment;

using System;
using System.Collections.Generic;

public sealed record ShipmentItemDto(
    Guid SkuId,
    int Quantity,
    string Name = "Product",
    decimal Price = 0,
    int Weight = 100,
    int Length = 10,
    int Width = 10,
    int Height = 10);

public sealed record ShipmentEventDto(
    Guid Id,
    string? CarrierStatus,
    string Status,
    string? Description,
    DateTimeOffset OccurredAt,
    string? ReasonCode = null,
    string? Reason = null,
    string? ShipperName = null,
    string? ShipperPhone = null,
    string? PodUrl = null,
    string? WarehouseName = null);

public sealed record ShipmentDto(
    Guid Id,
    Guid OrderId,
    Guid WarehouseId,
    Guid CarrierId,
    string? TrackingNumber,
    string Status,
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverAddress,
    Guid? BuyerId = null,
    string? ToProvinceName = null,
    int? ToDistrictId = null,
    string? ToWardCode = null,
    int Weight = 500,
    int Length = 20,
    int Width = 15,
    int Height = 10,
    decimal ShippingFee = 0,
    int CodAmount = 0,
    int InsuranceValue = 0,
    string? PrintToken = null,
    string? GhnOrderCode = null,
    DateTimeOffset? EstimatedDeliveryAt = null,
    DateTimeOffset? DeliveredAt = null,
    IReadOnlyList<ShipmentItemDto>? Items = null,
    IReadOnlyList<ShipmentEventDto>? Events = null,
    string? LastDeliveryFailureReason = null,
    string? LastDeliveryFailureCode = null,
    DateTimeOffset CreatedAt = default,
    DateTimeOffset UpdatedAt = default,
    string? SubOrderNo = null);

public sealed record CreateShipmentDto(
    Guid OrderId,
    Guid WarehouseId,
    Guid CarrierId,
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverAddress);

public sealed record ShipmentFilterRequest(
    Guid? WarehouseId = null,
    Guid? OrderId = null,
    string? TrackingNumber = null,
    string? Status = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    string? SortOrder = null);

public sealed record CancelShipmentResponse(
    Guid ShipmentId,
    string? TrackingNumber,
    bool IsSuccess,
    string Message);

public sealed record PrintShippingLabelResponse(
    Guid ShipmentId,
    string? TrackingNumber,
    string? Token,
    string PrintUrl,
    string PageSize);
