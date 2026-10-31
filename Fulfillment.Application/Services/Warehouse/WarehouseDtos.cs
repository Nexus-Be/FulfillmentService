namespace Fulfillment.Application.Services.Warehouse;

using System;
using System.ComponentModel.DataAnnotations;
using Fulfillment.Application.Common;

public sealed record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    bool IsActive,
    Guid? SellerId,
    DateTimeOffset CreatedAt);

public sealed record CreateWarehouseDto(
    [Required(ErrorMessage = "Warehouse code is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Warehouse code must be between 2 and 50 characters.")]
    string Code,

    [Required(ErrorMessage = "Warehouse name is required.")]
    [StringLength(200, ErrorMessage = "Warehouse name cannot exceed 200 characters.")]
    string Name,

    string? Address,
    Guid? SellerId)
{
    public string Code { get; init; } = StringNormalizer.NormalizeCode(Code);
    public string Name { get; init; } = StringNormalizer.NormalizeTitle(Name) ?? string.Empty;
    public string? Address { get; init; } = StringNormalizer.NormalizeTitle(Address);
}

public sealed record UpdateWarehouseDto(
    [Required(ErrorMessage = "Warehouse name is required.")]
    [StringLength(200, ErrorMessage = "Warehouse name cannot exceed 200 characters.")]
    string Name,

    string? Address)
{
    public string Name { get; init; } = StringNormalizer.NormalizeTitle(Name) ?? string.Empty;
    public string? Address { get; init; } = StringNormalizer.NormalizeTitle(Address);
}

public sealed record SetWarehouseActiveStatusDto(
    bool IsActive);
