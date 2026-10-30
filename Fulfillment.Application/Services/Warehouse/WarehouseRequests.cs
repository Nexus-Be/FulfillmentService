namespace Fulfillment.Application.Services.Warehouse;

using System;

public sealed record CreateWarehouseRequest(
    string Code,
    string Name,
    string Type,
    string? Address);
