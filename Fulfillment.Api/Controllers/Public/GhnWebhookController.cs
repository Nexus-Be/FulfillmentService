namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Infrastructure.Carriers.Ghn;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("webhooks/ghn")]
[Route("api/v1/webhooks/ghn")]
[Route("api/v1/fulfillment/webhook/ghn")]
[Route("api/v1/fulfillment/webhooks/ghn")]
public sealed class GhnWebhookController : ControllerBase
{
    private readonly IFulfillmentDbContext _dbContext;
    private readonly ICommerceWebhookClient _commerceWebhookClient;
    private readonly ILogger<GhnWebhookController> _logger;

    public GhnWebhookController(
        IFulfillmentDbContext dbContext,
        ICommerceWebhookClient commerceWebhookClient,
        ILogger<GhnWebhookController> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _commerceWebhookClient = commerceWebhookClient ?? throw new ArgumentNullException(nameof(commerceWebhookClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<IActionResult> HandleGhnCallback(
        [FromBody] GhnWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        if (payload == null || string.IsNullOrWhiteSpace(payload.OrderCode))
        {
            return BadRequest(new { message = "Invalid webhook payload." });
        }

        var rawJson = JsonSerializer.Serialize(payload);
        _logger.LogInformation("Received GHN Webhook for OrderCode: {OrderCode}, Status: {Status}", payload.OrderCode, payload.Status);

        // 1. Audit Log creation
        var auditLog = new WebhookAuditLog
        {
            OrderCode = payload.OrderCode,
            ClientOrderCode = payload.ClientOrderCode,
            EventType = payload.Type ?? "switch_status",
            Status = payload.Status,
            RawJson = rawJson,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsProcessed = false
        };
        _dbContext.WebhookAuditLogs.Add(auditLog);

        // 2. Strict Idempotency Check: if exact orderCode & status was already processed recently, return early
        var alreadyProcessed = await _dbContext.WebhookAuditLogs
            .AnyAsync(l => l.OrderCode == payload.OrderCode &&
                           l.Status == payload.Status &&
                           l.IsProcessed &&
                           l.ReceivedAt > DateTimeOffset.UtcNow.AddMinutes(-10),
                      cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Webhook event for OrderCode {OrderCode} with status {Status} already processed recently. Acknowledged.", payload.OrderCode, payload.Status);
            auditLog.IsProcessed = true;
            auditLog.ProcessMessage = "Duplicate webhook callback ignored (idempotent).";
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Duplicate webhook event acknowledged.", status = payload.Status });
        }

        // 3. Find shipment by tracking number or order code
        var shipment = await _dbContext.Shipments
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.TrackingNumber == payload.OrderCode ||
                s.GhnOrderCode == payload.OrderCode ||
                (payload.ClientOrderCode != null && s.OrderId.ToString() == payload.ClientOrderCode),
                cancellationToken);

        if (shipment == null)
        {
            _logger.LogWarning("Shipment with tracking {OrderCode} not found in database.", payload.OrderCode);
            auditLog.IsProcessed = false;
            auditLog.ProcessMessage = "Shipment not found in database.";
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Shipment not found, acknowledged." });
        }

        var newStatus = GhnStatusMapper.Map(payload.Status);
        shipment.Status = newStatus;
        shipment.UpdatedAt = DateTimeOffset.UtcNow;

        if (payload.ConvertedWeight.HasValue && payload.ConvertedWeight.Value > 0)
        {
            shipment.ConvertedWeight = payload.ConvertedWeight.Value;
        }

        if (newStatus == ShipmentStatus.Delivered)
        {
            shipment.DeliveredAt = DateTimeOffset.UtcNow;
        }

        // 4. Record ShipmentEvent with detailed extracted attributes
        var eventStatus = MapToEventStatus(newStatus);
        var shipmentEvent = new ShipmentEvent
        {
            ShipmentId = shipment.Id,
            CarrierStatus = payload.Status,
            Status = eventStatus,
            Description = payload.Description ?? $"Status updated to {newStatus}",
            ExternalEventId = payload.Type,
            OccurredAt = DateTimeOffset.UtcNow,
            RawPayload = rawJson,
            ReasonCode = payload.ReasonCode,
            Reason = payload.Reason,
            ShipperName = payload.ShipperName,
            ShipperPhone = payload.ShipperPhone,
            PodUrl = payload.PodUrl,
            WarehouseName = payload.Warehouse
        };
        _dbContext.ShipmentEvents.Add(shipmentEvent);

        auditLog.IsProcessed = true;
        auditLog.ProcessMessage = $"Successfully mapped to status '{newStatus}'.";

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 5. Notify CommerceService if one of the terminal/critical statuses
        var notifyStatuses = new[]
        {
            ShipmentStatus.HandedOver,
            ShipmentStatus.InTransit,
            ShipmentStatus.Delivered,
            ShipmentStatus.DeliveryFailed,
            ShipmentStatus.Returned,
            ShipmentStatus.Cancelled
        };

        if (Array.IndexOf(notifyStatuses, newStatus) >= 0)
        {
            try
            {
                await _commerceWebhookClient.NotifyDeliveryStatusAsync(
                    shipment.OrderId,
                    newStatus.ToString(),
                    shipment.TrackingNumber ?? payload.OrderCode,
                    payload.Description,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch delivery status notification to CommerceService for Order {OrderId}", shipment.OrderId);
            }
        }

        return Ok(new { message = "Webhook processed successfully.", status = newStatus.ToString() });
    }

    private static ShipmentEventStatus MapToEventStatus(ShipmentStatus status) => status switch
    {
        ShipmentStatus.ReadyToPick or ShipmentStatus.Packed => ShipmentEventStatus.Created,
        ShipmentStatus.Picking => ShipmentEventStatus.PickedUp,
        ShipmentStatus.InTransit or ShipmentStatus.HandedOver => ShipmentEventStatus.InTransit,
        ShipmentStatus.Delivered => ShipmentEventStatus.Delivered,
        ShipmentStatus.DeliveryFailed => ShipmentEventStatus.DeliveryFailed,
        ShipmentStatus.Returned => ShipmentEventStatus.Returned,
        _ => ShipmentEventStatus.InTransit
    };
}
