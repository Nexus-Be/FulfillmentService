namespace Fulfillment.Application.Services.Shipping;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

/// <summary>Where a location list came from: the carrier just now, or an earlier carrier answer still held in the cache.</summary>
public static class LocationSources
{
    public const string Ghn = "ghn";
    public const string GhnCache = "ghn-cache";
}

/// <summary>A province, district or ward list with its origin and the time the carrier gave it.</summary>
public sealed record LocationList<T>(IReadOnlyList<T> Items, string Source, DateTimeOffset ObservedAt);

public sealed record ProvinceDto(
    int ProvinceId,
    string ProvinceName,
    string? Code = null);

public sealed record DistrictDto(
    int DistrictId,
    int ProvinceId,
    string DistrictName,
    int SupportType = 0);

public sealed record WardDto(
    string WardCode,
    int DistrictId,
    string WardName);

public sealed record AvailableShippingServiceDto(
    int ServiceId,
    string ShortName,
    int ServiceTypeId);

public sealed record ValidateShippingLocationRequest(
    [Range(1, int.MaxValue)] int ProvinceId,
    [Range(1, int.MaxValue)] int DistrictId,
    [Required] string WardCode);

public sealed record ShippingLocationValidationResponse(
    bool IsValid,
    int ProvinceId,
    string? ProvinceName,
    int DistrictId,
    string? DistrictName,
    string? WardCode,
    string? WardName,
    string? Message = null);
