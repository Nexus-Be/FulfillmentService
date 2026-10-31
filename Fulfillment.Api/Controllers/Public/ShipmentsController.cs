namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Services.Shipment;
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

    /// <summary>Gets shipment details by tracking number.</summary>
    [HttpGet("tracking/{trackingNumber}")]
    public async Task<ActionResult<ShipmentDto>> GetByTrackingNumber([FromRoute] string trackingNumber, CancellationToken ct)
    {
        var shipment = await _shipmentService.GetByTrackingNumberAsync(trackingNumber, ct);
        return Ok(shipment);
    }

    /// <summary>Gets shipment details by order ID.</summary>
    [HttpGet("order/{orderId:guid}")]
    public async Task<ActionResult<ShipmentDto>> GetByOrderId([FromRoute] Guid orderId, CancellationToken ct)
    {
        var shipment = await _shipmentService.GetByOrderIdAsync(orderId, ct);
        return Ok(shipment);
    }
}
