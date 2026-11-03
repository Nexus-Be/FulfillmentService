namespace Fulfillment.Application.Services.Warehouse;

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public sealed record CreateWarehouseRequest(
    [Required(ErrorMessage = "Warehouse code is required.")]
    string Code,

    [Required(ErrorMessage = "Warehouse name is required.")]
    string Name,

    string? Type,

    string? Address,

    Guid? SellerId = null);

public sealed record UpdateWarehouseRequest(
    [Required(ErrorMessage = "Warehouse name is required.")]
    string Name,

    string? Address);

public sealed record InternalCreateWarehouseRequest(
    [property: JsonPropertyName("seller_id")]
    [Required(ErrorMessage = "Seller ID is required.")]
    Guid SellerId,

    [property: JsonPropertyName("user_id")]
    Guid UserId,

    [property: JsonPropertyName("shop_name")]
    [Required(ErrorMessage = "Shop name is required.")]
    string ShopName,

    [property: JsonPropertyName("contact_name")]
    string? ContactName,

    [property: JsonPropertyName("contact_phone")]
    string? ContactPhone,

    [property: JsonPropertyName("pickup_address")]
    [Required(ErrorMessage = "Pickup address is required.")]
    string PickupAddress,

    [property: JsonPropertyName("province_id")]
    int? ProvinceId,

    [property: JsonPropertyName("district_id")]
    int? DistrictId,

    [property: JsonPropertyName("ward_code")]
    string? WardCode);

public sealed record InternalWarehouseResponse(
    [property: JsonPropertyName("warehouse_id")]
    Guid WarehouseId,

    [property: JsonPropertyName("seller_id")]
    Guid SellerId,

    [property: JsonPropertyName("status")]
    string Status,

    [property: JsonPropertyName("created_at")]
    DateTimeOffset CreatedAt);
