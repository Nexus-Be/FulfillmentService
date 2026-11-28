namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Api.Extensions;
using Fulfillment.Api.Filters;
using Fulfillment.Application.Services.Shipment;
using Microsoft.AspNetCore.Authorization;
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
        var result = await _shipmentService.ListShipmentsAsync(request, userId, this.IsAdmin() || this.IsStaff(), ct);
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

    /// <summary>
    /// Provides an isolated printable thermal label HTML for Simulator tracking numbers.
    /// Does not depend on any third-party carrier API.
    /// </summary>
    [HttpGet("labels/simulator")]
    [AllowAnonymous]
    public IActionResult GetSimulatorPrintLabel([FromQuery] string tracking, [FromQuery] string? pageSize = "A5")
    {
        var safeTracking = !string.IsNullOrWhiteSpace(tracking) ? tracking : "SIM-DEMO-001";
        var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Phiếu Gửi Hàng - {safeTracking}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; padding: 20px; background: #f4f6f8; }}
        .label-card {{ max-width: 580px; margin: 0 auto; background: #fff; border: 2px dashed #1e293b; padding: 24px; border-radius: 8px; box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1); }}
        .header {{ display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #1e293b; padding-bottom: 12px; margin-bottom: 16px; }}
        .barcode {{ font-family: 'Courier New', Courier, monospace; font-size: 26px; font-weight: bold; letter-spacing: 5px; text-align: center; margin: 18px 0; padding: 12px; background: #f1f5f9; border-radius: 6px; border: 1px solid #cbd5e1; }}
        .section {{ margin-bottom: 12px; font-size: 14px; line-height: 1.5; color: #334155; }}
        .badge {{ display: inline-block; background: #0284c7; color: #fff; padding: 3px 8px; border-radius: 4px; font-weight: 600; font-size: 12px; }}
        .meta-table {{ width: 100%; border-collapse: collapse; margin-top: 12px; }}
        .meta-table td {{ padding: 6px 0; font-size: 13px; }}
        .footer {{ border-top: 1px dashed #cbd5e1; padding-top: 12px; margin-top: 18px; font-size: 12px; color: #64748b; text-align: center; }}
    </style>
</head>
<body>
    <div class=""label-card"">
        <div class=""header"">
            <div>
                <h2 style=""margin: 0 0 4px 0; color: #0f172a;"">NEXUS FULFILLMENT</h2>
                <span class=""badge"">VẬN ĐƠN MÔ PHỎNG (SIMULATOR)</span>
            </div>
            <div style=""text-align: right; font-size: 13px; color: #64748b;"">
                <div>Khổ in: <strong style=""color: #0f172a;"">{pageSize ?? "A5"}</strong></div>
                <div>Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm}</div>
            </div>
        </div>
        <div class=""barcode"">*{safeTracking}*</div>
        <table class=""meta-table"">
            <tr>
                <td style=""width: 35%; color: #64748b;"">Mã vận đơn:</td>
                <td><strong style=""font-size: 16px; color: #0f172a;"">{safeTracking}</strong></td>
            </tr>
            <tr>
                <td style=""color: #64748b;"">Đơn vị vận chuyển:</td>
                <td><strong>NEXUS SIMULATOR EXPRESS</strong></td>
            </tr>
            <tr>
                <td style=""color: #64748b;"">Trạng thái:</td>
                <td><span style=""color: #16a34a; font-weight: 600;"">SẴN SÀNG LẤY HÀNG (READY TO PICK)</span></td>
            </tr>
            <tr>
                <td style=""color: #64748b;"">Phương thức thanh toán:</td>
                <td>Đã thanh toán / COD chuẩn theo đơn</td>
            </tr>
        </table>
        <div class=""footer"">
            Phiếu gửi hàng điện tử được tạo bởi Nexus Fulfillment Service &bull; Hệ thống mô phỏng nội bộ độc lập.
        </div>
    </div>
</body>
</html>";
        return Content(html, "text/html; charset=utf-8");
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

    /// <summary>Gets shipment details by sub-order or auction-order ID.</summary>
    [HttpGet("sub-orders/{subOrderId:guid}")]
    [RequireRole("buyer", "seller", "admin", "staff")]
    public async Task<ActionResult<ShipmentDto>> GetBySubOrderId([FromRoute] Guid subOrderId, CancellationToken ct)
    {
        var userId = this.GetUserId() ?? Guid.Empty;
        var isAdminOrStaff = this.IsAdmin() || this.IsStaff();
        var shipment = await _shipmentService.GetByOrderIdAsync(subOrderId, userId, isAdminOrStaff, ct);
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
