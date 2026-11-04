namespace Fulfillment.Api.Controllers.Internal;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Application.Services.Shipping;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/internal/shipments")]
public sealed class InternalShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public InternalShipmentsController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService ?? throw new ArgumentNullException(nameof(shipmentService));
    }

    [HttpPost]
    public async Task<ActionResult<CreateShipmentResponse>> CreateShipment(
        [FromBody] CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _shipmentService.CreateShipmentAsync(request, cancellationToken);
        return Ok(result);
    }
}

