namespace Fulfillment.Infrastructure.Carriers.Ghn;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public sealed class GhnApiResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("code_message")]
    public string? CodeMessage { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public sealed class GhnFeeCalculationRequest
{
    [JsonPropertyName("from_district_id")]
    public int FromDistrictId { get; set; }

    [JsonPropertyName("to_district_id")]
    public int ToDistrictId { get; set; }

    [JsonPropertyName("to_ward_code")]
    public string ToWardCode { get; set; } = string.Empty;

    [JsonPropertyName("service_type_id")]
    public int ServiceTypeId { get; set; } = 2;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 10;

    [JsonPropertyName("length")]
    public int Length { get; set; } = 20;

    [JsonPropertyName("width")]
    public int Width { get; set; } = 15;

    [JsonPropertyName("weight")]
    public int Weight { get; set; } = 500;

    [JsonPropertyName("insurance_value")]
    public int InsuranceValue { get; set; }

    [JsonPropertyName("cod_failed_amount")]
    public int CodFailedAmount { get; set; }
}

public sealed class GhnFeeData
{
    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("service_fee")]
    public decimal ServiceFee { get; set; }

    [JsonPropertyName("insurance_fee")]
    public decimal InsuranceFee { get; set; }
}

public sealed class GhnItemDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public int Price { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; } = 10;

    [JsonPropertyName("width")]
    public int Width { get; set; } = 10;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 10;

    [JsonPropertyName("weight")]
    public int Weight { get; set; } = 100;
}

public sealed class GhnCreateOrderRequest
{
    [JsonPropertyName("payment_type_id")]
    public int PaymentTypeId { get; set; } = 1; // 1: Shop tra, 2: Nguoi nhan tra

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("required_note")]
    public string RequiredNote { get; set; } = "CHOXEMHANGKHONGTHU"; // KHONGCHOXEMHANG | CHOXEMHANGKHONGTHU | CHOTHUHANG

    [JsonPropertyName("from_name")]
    public string? FromName { get; set; }

    [JsonPropertyName("from_phone")]
    public string? FromPhone { get; set; }

    [JsonPropertyName("from_address")]
    public string? FromAddress { get; set; }

    [JsonPropertyName("from_ward_name")]
    public string? FromWardName { get; set; }

    [JsonPropertyName("from_district_id")]
    public int? FromDistrictId { get; set; }

    [JsonPropertyName("to_name")]
    public string ToName { get; set; } = string.Empty;

    [JsonPropertyName("to_phone")]
    public string ToPhone { get; set; } = string.Empty;

    [JsonPropertyName("to_address")]
    public string ToAddress { get; set; } = string.Empty;

    [JsonPropertyName("to_ward_name")]
    public string? ToWardName { get; set; }

    [JsonPropertyName("to_ward_code")]
    public string? ToWardCode { get; set; }

    [JsonPropertyName("to_district_id")]
    public int? ToDistrictId { get; set; }

    [JsonPropertyName("weight")]
    public int Weight { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; } = 20;

    [JsonPropertyName("width")]
    public int Width { get; set; } = 15;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 10;

    [JsonPropertyName("service_type_id")]
    public int ServiceTypeId { get; set; } = 2; // 2: < 20kg; 5: >= 20kg

    [JsonPropertyName("cod_amount")]
    public int CodAmount { get; set; }

    [JsonPropertyName("client_order_code")]
    public string ClientOrderCode { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<GhnItemDto> Items { get; set; } = new();
}

public sealed class GhnCreateOrderData
{
    [JsonPropertyName("order_code")]
    public string OrderCode { get; set; } = string.Empty;

    [JsonPropertyName("total_fee")]
    public decimal TotalFee { get; set; }

    [JsonPropertyName("expected_delivery_time")]
    public string? ExpectedDeliveryTime { get; set; }
}

public sealed class GhnOrderDetailData
{
    [JsonPropertyName("order_code")]
    public string OrderCode { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("updated_date")]
    public string? UpdatedDate { get; set; }
}

public sealed class GhnCancelOrderRequest
{
    [JsonPropertyName("order_codes")]
    public List<string> OrderCodes { get; set; } = new();
}

public sealed class GhnWebhookPayload
{
    [JsonPropertyName("OrderCode")]
    public string OrderCode { get; set; } = string.Empty;

    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("ClientOrderCode")]
    public string? ClientOrderCode { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("Fee")]
    public decimal? Fee { get; set; }

    [JsonPropertyName("TotalFee")]
    public decimal? TotalFee { get; set; }

    [JsonPropertyName("Time")]
    public object? Time { get; set; }
}
