namespace Fulfillment.Application.Services.Warehouse;

using System;

public sealed record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    string? Address,
    bool IsDisabled);
