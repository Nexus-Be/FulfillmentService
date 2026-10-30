namespace Fulfillment.Application.Services.Warehouse;

using System;

public sealed record WarehouseResponse(
    Guid Id,
    string Code,
    string Name,
    string Type,
    string? Address,
    bool IsDisabled);
