namespace Fulfillment.Application.Services.Shipping;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public sealed record ShippingQuoteItem(
    [Required] Guid SkuId,
    [Range(1, int.MaxValue)] int Quantity,
    [Range(1, int.MaxValue)] int WeightInGrams);

public sealed record ShopShippingQuoteRequest(
    [Required] Guid SellerId,
    // 0 means "not given": the service then uses the district of the seller's warehouse, so callers that do not know it (Commerce) can omit it.
    [Range(0, int.MaxValue)] int FromDistrictId,
    [Range(1, int.MaxValue)] int ToDistrictId,
    [Required] string ToWardCode,
    [Range(1, int.MaxValue)] int WeightInGrams,
    IReadOnlyList<ShippingQuoteItem>? Items = null,
    string? CarrierCode = null);

public sealed record ShippingQuoteResponse(
    decimal Fee,
    bool IsEstimatedFallback,
    string ServiceName);
