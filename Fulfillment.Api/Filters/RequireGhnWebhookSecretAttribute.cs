namespace Fulfillment.Api.Filters;

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// The GHN webhook can mark a shipment delivered, which lets the buyer confirm receipt and releases the seller's payout, or
/// cancel an order and queue a refund. It is public, so it must prove the caller knows a shared secret. GHN lets us configure
/// only the callback URL, so the secret is accepted in the X-Webhook-Token header or in the "token" query parameter.
/// With no secret configured the webhook refuses every call (there is no built-in default).
/// </summary>
public sealed class RequireGhnWebhookSecretAttribute : TypeFilterAttribute
{
    public RequireGhnWebhookSecretAttribute() : base(typeof(GhnWebhookSecretFilter)) { }
}

public sealed class GhnWebhookSecretFilter(IConfiguration configuration) : IAsyncAuthorizationFilter
{
    public const string HeaderName = "X-Webhook-Token";
    public const string QueryName = "token";

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var expected = configuration["Carriers:GHN:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            context.Result = new ObjectResult(new
            {
                errorCode = "WEBHOOK_NOT_CONFIGURED",
                message = "The carrier webhook secret is not configured."
            })
            { StatusCode = StatusCodes.Status503ServiceUnavailable };
            return Task.CompletedTask;
        }

        var request = context.HttpContext.Request;
        var provided = request.Headers[HeaderName].ToString();
        if (string.IsNullOrEmpty(provided))
            provided = request.Query[QueryName].ToString();

        if (string.IsNullOrEmpty(provided) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected)))
        {
            context.Result = new ObjectResult(new
            {
                errorCode = "WEBHOOK_UNAUTHORIZED",
                message = "A valid webhook token is required."
            })
            { StatusCode = StatusCodes.Status401Unauthorized };
        }

        return Task.CompletedTask;
    }
}
