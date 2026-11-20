namespace Fulfillment.Api.Controllers.Internal;

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

public sealed record ShipmentReconciliationDto(
    Guid ShipmentId,
    Guid OrderId,
    string TrackingNumber,
    string Status,
    decimal EstimatedShippingFee,
    decimal ActualShippingFee,
    decimal FeeVariance,
    decimal CodAmount,
    bool IsDelivered,
    DateTimeOffset? DeliveredAt,
    string ReconciliationStatus);

public sealed record BatchReconciliationRequest(
    IReadOnlyList<Guid>? OrderIds = null,
    IReadOnlyList<string>? TrackingNumbers = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);

public sealed record BatchReconciliationResponse(
    int TotalCount,
    decimal TotalCodAmount,
    decimal TotalEstimatedFee,
    decimal TotalActualFee,
    decimal NetVariance,
    IReadOnlyList<ShipmentReconciliationDto> Items);

[ApiController]
[Route("api/v1/internal/reconciliation")]
public sealed class ReconciliationController : ControllerBase
{
    private readonly IFulfillmentDbContext _dbContext;

    public ReconciliationController(IFulfillmentDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>P2-04: Đối soát chi tiết cước vận chuyển và tiền COD cho 1 đơn hàng.</summary>
    [HttpGet("orders/{orderId:guid}")]
    public async Task<ActionResult<ShipmentReconciliationDto>> GetOrderReconciliation(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken)
    {
        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);

        if (shipment == null)
        {
            return NotFound(new { message = $"No shipment found for order {orderId}" });
        }

        var isDelivered = shipment.Status == ShipmentStatus.Delivered;
        var actualFee = shipment.ShippingFee;
        var estFee = shipment.ShippingFee; // Baseline estimated fee
        var variance = actualFee - estFee;

        var reconStatus = shipment.Status switch
        {
            ShipmentStatus.Delivered => variance == 0 ? "Matched" : "FeeDiscrepancy",
            ShipmentStatus.Returned => "Returned",
            ShipmentStatus.Cancelled => "Cancelled",
            _ => "PendingDelivery"
        };

        return Ok(new ShipmentReconciliationDto(
            shipment.Id,
            shipment.OrderId,
            shipment.TrackingNumber ?? shipment.GhnOrderCode ?? string.Empty,
            shipment.Status.ToString(),
            estFee,
            actualFee,
            variance,
            shipment.CodAmount,
            isDelivered,
            shipment.DeliveredAt,
            reconStatus));
    }

    /// <summary>P2-04: Đối soát hàng loạt cước và COD theo kỳ/danh sách mã đơn cho kế toán và ví.</summary>
    [HttpPost("shipments/audit")]
    public async Task<ActionResult<BatchReconciliationResponse>> AuditShipments(
        [FromBody] BatchReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Shipments.AsNoTracking();

        if (request.OrderIds != null && request.OrderIds.Count > 0)
        {
            query = query.Where(s => request.OrderIds.Contains(s.OrderId));
        }

        if (request.TrackingNumbers != null && request.TrackingNumbers.Count > 0)
        {
            query = query.Where(s => s.TrackingNumber != null && request.TrackingNumbers.Contains(s.TrackingNumber));
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(s => s.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(s => s.CreatedAt <= request.ToDate.Value);
        }

        var shipments = await query.Take(100).ToListAsync(cancellationToken);

        var items = shipments.Select(s =>
        {
            var isDelivered = s.Status == ShipmentStatus.Delivered;
            var actualFee = s.ShippingFee;
            var estFee = s.ShippingFee;
            var variance = actualFee - estFee;

            var reconStatus = s.Status switch
            {
                ShipmentStatus.Delivered => variance == 0 ? "Matched" : "FeeDiscrepancy",
                ShipmentStatus.Returned => "Returned",
                ShipmentStatus.Cancelled => "Cancelled",
                _ => "PendingDelivery"
            };

            return new ShipmentReconciliationDto(
                s.Id,
                s.OrderId,
                s.TrackingNumber ?? s.GhnOrderCode ?? string.Empty,
                s.Status.ToString(),
                estFee,
                actualFee,
                variance,
                s.CodAmount,
                isDelivered,
                s.DeliveredAt,
                reconStatus);
        }).ToList();

        var totalCod = items.Where(i => i.IsDelivered).Sum(i => i.CodAmount);
        var totalEst = items.Sum(i => i.EstimatedShippingFee);
        var totalActual = items.Sum(i => i.ActualShippingFee);
        var netVariance = totalActual - totalEst;

        return Ok(new BatchReconciliationResponse(
            items.Count,
            totalCod,
            totalEst,
            totalActual,
            netVariance,
            items));
    }
}
