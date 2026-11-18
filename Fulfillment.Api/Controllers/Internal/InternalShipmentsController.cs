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

    /// <summary>Internal cancel endpoint by order ID (subOrderId) for CommerceService.</summary>
    [HttpPost("order/{orderId:guid}/cancel")]
    public async Task<ActionResult<CancelShipmentResponse>> CancelByOrderId(
        [FromRoute] Guid orderId,
        [FromBody] CancelShipmentRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _shipmentService.CancelShipmentByOrderIdAsync(orderId, request?.Reason, cancellationToken);
        return Ok(result);
    }

    /// <summary>Internal cancel endpoint by shipment ID.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<CancelShipmentResponse>> CancelById(
        [FromRoute] Guid id,
        [FromBody] CancelShipmentRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _shipmentService.CancelShipmentAsync(id, request?.Reason, Guid.Empty, isAdmin: true, cancellationToken);
        return Ok(result);
    }

    /// <summary>Internal lookup endpoint by order ID for CommerceService reconciliation.</summary>
    [HttpGet("order/{orderId:guid}")]
    public async Task<ActionResult<ShipmentDto>> GetByOrderId(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken)
    {
        var result = await _shipmentService.GetByOrderIdAsync(orderId, Guid.Empty, isAdminOrStaff: true, cancellationToken);
        return Ok(result);
    }

    /// <summary>Internal lookup endpoint by shipment ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShipmentDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _shipmentService.GetByIdAsync(id, Guid.Empty, isAdminOrStaff: true, cancellationToken);
        return Ok(result);
    }

    /// <summary>Internal update recipient endpoint for changing address post-order.</summary>
    [HttpPut("order/{orderId:guid}/recipient")]
    public async Task<ActionResult<ShipmentDto>> UpdateRecipient(
        [FromRoute] Guid orderId,
        [FromBody] UpdateRecipientAddressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _shipmentService.UpdateRecipientAddressAsync(orderId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Internal sync status endpoint by order ID.</summary>
    [HttpPost("order/{orderId:guid}/sync-status")]
    public async Task<ActionResult<ShipmentDto>> SyncStatusByOrderId(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken)
    {
        var result = await _shipmentService.SyncStatusByOrderIdAsync(orderId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Internal sync status endpoint by shipment ID.</summary>
    [HttpPost("{id:guid}/sync-status")]
    public async Task<ActionResult<ShipmentDto>> SyncStatusById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _shipmentService.SyncStatusAsync(id, Guid.Empty, isAdminOrStaff: true, cancellationToken);
        return Ok(result);
    }
}
