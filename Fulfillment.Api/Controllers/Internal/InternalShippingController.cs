namespace Fulfillment.Api.Controllers.Internal;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Application.Services.Shipping;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/internal/shipping")]
public sealed class InternalShippingController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public InternalShippingController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService ?? throw new ArgumentNullException(nameof(shipmentService));
    }

    [HttpPost("quote")]
    public async Task<ActionResult<ShippingQuoteResponse>> Quote(
        [FromBody] ShopShippingQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _shipmentService.CalculateShippingFeeAsync(request, cancellationToken);
        return Ok(result);
    }
}

