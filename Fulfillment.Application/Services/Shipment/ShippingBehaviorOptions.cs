namespace Fulfillment.Application.Services.Shipment;

/// <summary>Switches for the returning flow. Both are off unless configured, so nothing new reaches Commerce by accident.</summary>
/// <param name="EmitReturningStatus">
/// Flag A, <c>Shipping:EmitReturningStatus</c>: tell Commerce when a parcel starts coming back ("Returning"). Turn it on only after Commerce accepts that status.
/// </param>
/// <param name="AutoReturnAfterFailedAttempts">
/// Flag B, <c>Shipping:AutoReturnAfterFailedAttempts</c>: move a shipment to Returning on its third failed delivery attempt. Keep it off in production
/// until the carrier is confirmed to repeat the event time of a retried webhook (otherwise one failure could be counted twice).
/// </param>
public sealed record ShippingBehaviorOptions(bool EmitReturningStatus = false, bool AutoReturnAfterFailedAttempts = false)
{
    /// <summary>Failed delivery attempts after which a shipment is sent back.</summary>
    public const int MaxFailedAttempts = 3;
}
