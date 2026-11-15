namespace Fulfillment.Application.Services.Warehouse;

using System;

public sealed record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    string? ContactPhone,
    string? ProvinceName,
    int? DistrictId,
    string? WardCode,
    int? GhnShopId,
    bool IsDisabled,
    Guid? SellerId = null);
