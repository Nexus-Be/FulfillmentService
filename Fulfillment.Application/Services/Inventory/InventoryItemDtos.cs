namespace Fulfillment.Application.Services.Inventory;

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fulfillment.Domain.Enums;

public sealed record InventoryItemResponse(
    Guid Id,
    Guid SkuId,
    Guid WarehouseId,
    int TotalQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    DateTimeOffset UpdatedAt);

public sealed record DeclareInventoryRequest(
    [Required(ErrorMessage = "SKU ID is required.")]
    Guid SkuId,

    [Required(ErrorMessage = "Warehouse ID is required.")]
    Guid WarehouseId,

    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 0.")]
    int Quantity,

    string? Note = null);

public sealed record AdjustInventoryRequest(
    [Required(ErrorMessage = "SKU ID is required.")]
    Guid SkuId,

    [Required(ErrorMessage = "Adjustment type is required.")]
    [property: JsonConverter(typeof(InventoryAdjustmentTypeJsonConverter))]
    InventoryAdjustmentType Type,

    int Quantity,

    string? Note = null);

public sealed record AdjustInventoryResponse(
    Guid SkuId,
    int TotalQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    Guid AdjustmentLedgerId);

public sealed class InventoryAdjustmentTypeJsonConverter : JsonConverter<InventoryAdjustmentType>
{
    public override InventoryAdjustmentType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var intValue = reader.GetInt32();
            return intValue switch
            {
                1 => InventoryAdjustmentType.Intake,
                2 => InventoryAdjustmentType.Adjust,
                _ => throw new JsonException($"Invalid adjustment type value: {intValue}. Allowed values are 1 (Intake) or 2 (Adjust).")
            };
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                throw new JsonException("Adjustment type cannot be empty.");
            }

            if (stringValue.Equals("Intake", StringComparison.OrdinalIgnoreCase) || stringValue == "1")
            {
                return InventoryAdjustmentType.Intake;
            }

            if (stringValue.Equals("Adjust", StringComparison.OrdinalIgnoreCase) || stringValue == "2")
            {
                return InventoryAdjustmentType.Adjust;
            }

            throw new JsonException($"Invalid adjustment type: '{stringValue}'. Allowed values are 'Intake' (1) or 'Adjust' (2).");
        }

        throw new JsonException($"Unexpected token {reader.TokenType} parsing InventoryAdjustmentType.");
    }

    public override void Write(Utf8JsonWriter writer, InventoryAdjustmentType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
