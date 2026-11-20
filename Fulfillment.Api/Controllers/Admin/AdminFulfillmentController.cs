namespace Fulfillment.Api.Controllers.Admin;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public sealed record FulfillmentDashboardResponse(
    int TotalShipments,
    int ReadyToPickCount,
    int InTransitCount,
    int DeliveredCount,
    int FailedCount,
    int ReturnedCount,
    int CancelledCount,
    decimal SuccessRatePercent,
    decimal ReturnRatePercent,
    decimal TotalCodCollected,
    decimal TotalShippingFees);

public sealed record WebhookAuditLogDto(
    Guid Id,
    string? OrderCode,
    string? ClientOrderCode,
    string? EventType,
    string? Status,
    bool IsProcessed,
    string? ProcessMessage,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? CommerceNotifiedAt,
    int CommerceNotifyAttempts);

public sealed record PagedWebhookAuditsResponse(
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<WebhookAuditLogDto> Items);

[ApiController]
[Route("api/v1/admin/fulfillment")]
public sealed class AdminFulfillmentController : ControllerBase
{
    private readonly IFulfillmentDbContext _dbContext;

    public AdminFulfillmentController(IFulfillmentDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>P2-08: Dashboard vận hành tổng quan toàn sàn về giao vận và kho bãi.</summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<FulfillmentDashboardResponse>> GetDashboard(CancellationToken cancellationToken)
    {
        var shipments = await _dbContext.Shipments
            .AsNoTracking()
            .Select(s => new { s.Status, s.ShippingFee, s.CodAmount })
            .ToListAsync(cancellationToken);

        var total = shipments.Count;
        var ready = shipments.Count(s => s.Status is ShipmentStatus.ReadyToPick or ShipmentStatus.Picking or ShipmentStatus.Packed);
        var inTransit = shipments.Count(s => s.Status is ShipmentStatus.HandedOver or ShipmentStatus.InTransit);
        var delivered = shipments.Count(s => s.Status == ShipmentStatus.Delivered);
        var failed = shipments.Count(s => s.Status == ShipmentStatus.DeliveryFailed);
        var returned = shipments.Count(s => s.Status == ShipmentStatus.Returned);
        var cancelled = shipments.Count(s => s.Status == ShipmentStatus.Cancelled);

        var successRate = total > 0 ? Math.Round((decimal)delivered / total * 100, 2) : 0m;
        var returnRate = total > 0 ? Math.Round((decimal)returned / total * 100, 2) : 0m;

        var totalCod = shipments.Where(s => s.Status == ShipmentStatus.Delivered).Sum(s => s.CodAmount);
        var totalFees = shipments.Sum(s => s.ShippingFee);

        return Ok(new FulfillmentDashboardResponse(
            total,
            ready,
            inTransit,
            delivered,
            failed,
            returned,
            cancelled,
            successRate,
            returnRate,
            totalCod,
            totalFees));
    }

    /// <summary>P2-08: Tra cứu nhật ký webhook audit logs để đối soát lỗi và giám sát giao vận.</summary>
    [HttpGet("webhooks/audit")]
    public async Task<ActionResult<PagedWebhookAuditsResponse>> GetWebhookAudits(
        [FromQuery] string? orderCode = null,
        [FromQuery] bool? isProcessed = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WebhookAuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(orderCode))
        {
            query = query.Where(w => w.OrderCode != null && w.OrderCode.Contains(orderCode));
        }

        if (isProcessed.HasValue)
        {
            query = query.Where(w => w.IsProcessed == isProcessed.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(w => w.ReceivedAt)
            .Skip((Math.Max(page, 1) - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WebhookAuditLogDto(
                w.Id,
                w.OrderCode,
                w.ClientOrderCode,
                w.EventType,
                w.Status,
                w.IsProcessed,
                w.ProcessMessage,
                w.ReceivedAt,
                w.CommerceNotifiedAt,
                w.CommerceNotifyAttempts))
            .ToListAsync(cancellationToken);

        return Ok(new PagedWebhookAuditsResponse(total, page, pageSize, items));
    }
}
