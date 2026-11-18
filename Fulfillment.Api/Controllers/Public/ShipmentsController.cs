namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Api.Extensions;
using Fulfillment.Api.Filters;
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

    /// <summary>
    /// Gets a paginated list of shipments with multi-criteria filtering and seller isolation.
    /// </summary>
    [HttpGet]
    [RequireRole("buyer", "seller", "admin", "staff")]
    public async Task<IActionResult> ListShipments(
        [FromQuery] ShipmentFilterRequest request,
        CancellationToken ct)
    {
        var userId = this.GetUserId() ?? Guid.Empty;
        var result = await _shipmentService.ListShipmentsAsync(request, userId, this.IsAdmin(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Cancels a shipment and synchronizes cancellation with the carrier.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [RequireRole("seller", "admin")]
    public async Task<IActionResult> CancelShipment(
        [FromRoute] Guid id,
        [FromBody] CancelShipmentRequest? request,
        CancellationToken ct)
    {
        var userId = this.GetUserId() ?? Guid.Empty;
        var result = await _shipmentService.CancelShipmentAsync(id, request?.Reason, userId, this.IsAdmin(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Generates or retrieves shipping label print token and formatted URL for thermal printing.
    /// </summary>
    [HttpGet("{id:guid}/print-label")]
    [RequireRole("seller", "admin", "staff")]
    public async Task<IActionResult> GetPrintLabel(
        [FromRoute] Guid id,
        [FromQuery] string pageSize = "A5",
        CancellationToken ct = default)
    {
        var userId = this.GetUserId() ?? Guid.Empty;
        var result = await _shipmentService.GetPrintLabelAsync(id, pageSize, userId, this.IsAdmin(), ct);
        return Ok(result);
    }

    /// <summary>Gets shipment details by shipment ID.</summary>
    [HttpGet("{id:guid}")]
    [RequireRole("buyer", "seller", "admin", "staff")]
    public async Task<ActionResult<ShipmentDto>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var userId = this.GetUserId() ?? Guid.Empty;
        var isAdminOrStaff = this.IsAdmin() || this.IsStaff();
        var shipment = await _shipmentService.GetByIdAsync(id, userId, isAdminOrStaff, ct);
        return Ok(shipment);
    }

    /// <summary>Gets shipment details by tracking number.</summary>
    [HttpGet("tracking/{trackingNumber}")]
    [RequireRole("buyer", "seller", "admin", "staff")]
    public async Task<ActionResult<ShipmentDto>> GetByTrackingNumber([FromRoute] string trackingNumber, CancellationToken ct)
    {
        var userId = this.GetUserId() ?? Guid.Empty;
        var isAdminOrStaff = this.IsAdmin() || this.IsStaff();
        var shipment = await _shipmentService.GetByTrackingNumberAsync(trackingNumber, userId, isAdminOrStaff, ct);
        return Ok(shipment);
    }

    /// <summary>Gets shipment details by order ID.</summary>
    [HttpGet("order/{orderId:guid}")]
    [RequireRole("buyer", "seller", "admin", "staff")]
    public async Task<ActionResult<ShipmentDto>> GetByOrderId([FromRoute] Guid orderId, CancellationToken ct)
    {
        var userId = this.GetUserId() ?? Guid.Empty;
        var isAdminOrStaff = this.IsAdmin() || this.IsStaff();
        var shipment = await _shipmentService.GetByOrderIdAsync(orderId, userId, isAdminOrStaff, ct);
        return Ok(shipment);
    }

    /// <summary>Synchronizes latest shipment status directly from GHN carrier gateway.</summary>
    [HttpPost("{id:guid}/sync-status")]
    [RequireRole("seller", "admin", "staff")]
    public async Task<ActionResult<ShipmentDto>> SyncStatus([FromRoute] Guid id, CancellationToken ct)
    {
        var userId = this.GetUserId() ?? Guid.Empty;
        var isAdminOrStaff = this.IsAdmin() || this.IsStaff();
        var shipment = await _shipmentService.SyncStatusAsync(id, userId, isAdminOrStaff, ct);
        return Ok(shipment);
    }

    /// <summary>
    /// [P2-07] Seller confirms received returned parcel and automatically restores inventory stock.
    /// </summary>
    [HttpPost("/api/v1/seller/orders/{orderId:guid}/confirm-return")]
    [HttpPost("orders/{orderId:guid}/confirm-return")]
    [RequireRole("seller", "admin")]
    public async Task<IActionResult> ConfirmReturn(
        [FromRoute] Guid orderId,
        CancellationToken ct)
    {
        var userId = this.GetUserId() ?? Guid.Empty;
        var result = await _shipmentService.ConfirmReturnAsync(orderId, userId, this.IsAdmin(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Simulates courier/shipper status milestone updates for sandbox testing.
    /// Updates shipment lifecycle, logs events and audit logs, and dispatches updates to Commerce and Notification services.
    /// </summary>
    [HttpPost("{trackingNumber}/simulate-shipper-status")]
    [HttpPost("simulate-shipper-status")]
    [RequireRole("seller", "admin", "staff")]
    public async Task<IActionResult> SimulateShipperStatus(
        [FromRoute] string? trackingNumber,
        [FromBody] SimulateShipperStatusRequest request,
        CancellationToken ct)
    {
        var result = await _shipmentService.SimulateShipperStatusAsync(request, trackingNumber, ct);
        return Ok(result);
    }
}
