namespace Fulfillment.Application.Services.Shipping;

/// <summary>
/// Opt-in estimate for the shipping fee when the carrier cannot be asked because its API token is not configured.
/// Off by default: a missing token then fails the quote. It exists so a development environment without carrier credentials
/// can still run an order through payment. The response is marked <c>IsEstimatedFallback</c> so it is never mistaken for a real quote.
/// </summary>
public sealed record ShippingEstimateOptions(bool Enabled = false, decimal BaseFee = 30000m, decimal PerExtra500Grams = 5000m)
{
    public decimal Estimate(int weightInGrams)
    {
        var extraBlocks = weightInGrams <= 500 ? 0 : (int)Math.Ceiling((weightInGrams - 500) / 500d);
        return BaseFee + extraBlocks * PerExtra500Grams;
    }
}
