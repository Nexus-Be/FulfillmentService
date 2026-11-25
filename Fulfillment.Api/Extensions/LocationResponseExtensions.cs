namespace Fulfillment.Api.Extensions;

using System.Globalization;
using Fulfillment.Application.Services.Shipping;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Location lists keep a plain array as the body, so every caller written before the source was reported keeps working. The origin of the
/// list and the time the carrier gave it travel in two headers, which the Gateway lists as readable by the browser.
/// </summary>
public static class LocationResponseExtensions
{
    public const string SourceHeader = "X-Location-Source";
    public const string ObservedAtHeader = "X-Location-Observed-At";

    public static OkObjectResult OkLocations<T>(this ControllerBase controller, LocationList<T> list)
    {
        controller.Response.Headers[SourceHeader] = list.Source;
        controller.Response.Headers[ObservedAtHeader] = list.ObservedAt.ToString("O", CultureInfo.InvariantCulture);
        return controller.Ok(list.Items);
    }
}
