namespace Fulfillment.Api.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Services.Shipment;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/shipments")]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public ShipmentsController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }

    [HttpGet("tracking/{trackingNumber}")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByTrackingNumber([FromRoute] string trackingNumber, CancellationToken ct)
    {
        var shipment = await _shipmentService.GetByTrackingNumberAsync(trackingNumber, ct);
        if (shipment == null)
        {
            return NotFound(new { message = "Shipment not found with the provided tracking number." });
        }

        return Ok(shipment);
    }

    [HttpGet("order/{orderId:guid}")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByOrderId([FromRoute] Guid orderId, CancellationToken ct)
    {
        var shipment = await _shipmentService.GetByOrderIdAsync(orderId, ct);
        if (shipment == null)
        {
            return NotFound(new { message = "Shipment not found for the specified order." });
        }

        return Ok(shipment);
    }
}
