namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Fulfillment.Api.Extensions;
using Fulfillment.Application.Services.Shipment;

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
    public async Task<IActionResult> GetByTrackingNumber([FromRoute] string trackingNumber, CancellationToken ct)
    {
        var shipment = await _shipmentService.GetByTrackingNumberAsync(trackingNumber, ct);
        if (shipment == null)
        {
            return this.NotFoundError("SHIPMENT_NOT_FOUND", $"Shipment not found with tracking number '{trackingNumber}'.");
        }

        return Ok(shipment);
    }

    [HttpGet("order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrderId([FromRoute] Guid orderId, CancellationToken ct)
    {
        var shipment = await _shipmentService.GetByOrderIdAsync(orderId, ct);
        if (shipment == null)
        {
            return this.NotFoundError("SHIPMENT_NOT_FOUND", $"Shipment not found for order ID '{orderId}'.");
        }

        return Ok(shipment);
    }
}
