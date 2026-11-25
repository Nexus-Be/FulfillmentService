namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Api.Filters;
using Fulfillment.Application.Interfaces;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Infrastructure.Carriers.Ghn;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("webhooks/ghn")]
[Route("api/v1/webhooks/ghn")]
[Route("api/v1/webhooks/carriers/ghn")]
[Route("api/v1/fulfillment/webhook/ghn")]
[Route("api/v1/fulfillment/webhooks/ghn")]
[RequireGhnWebhookSecret]
public sealed class GhnWebhookController : ControllerBase
{
    private readonly IFulfillmentDbContext _dbContext;
    private readonly DeliveryNotificationOutbox _outbox;
    private readonly ILogger<GhnWebhookController> _logger;

    public GhnWebhookController(
        IFulfillmentDbContext dbContext,
        DeliveryNotificationOutbox outbox,
        ILogger<GhnWebhookController> logger,
        INotificationClient? notificationClient = null,
        ShippingBehaviorOptions? behavior = null)
    {
        _behavior = behavior ?? new ShippingBehaviorOptions();
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationClient = notificationClient;
    }

    private readonly INotificationClient? _notificationClient;
    private readonly ShippingBehaviorOptions _behavior;

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

        // The carrier event time tells a repeated delivery of one event apart from a genuinely new one (for example a second failed attempt).
        var eventTimeKey = GhnEventTime.Key(payload.Time);

        // 1. Audit Log creation
        var auditLog = new WebhookAuditLog
        {
            EventTimeKey = eventTimeKey,
            OrderCode = payload.OrderCode,
            ClientOrderCode = payload.ClientOrderCode,
            EventType = payload.Type ?? "switch_status",
            Status = payload.Status,
            RawJson = rawJson,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsProcessed = false
        };
        _dbContext.WebhookAuditLogs.Add(auditLog);

        // 2. Strict Idempotency Check: the same order code, status AND event time was already processed, so return early.
        //    Without a usable event time only the order code and status can be compared (the old behaviour).
        var alreadyProcessed = eventTimeKey is null
            ? await _dbContext.WebhookAuditLogs
                .AnyAsync(l => l.OrderCode == payload.OrderCode &&
                               l.Status == payload.Status &&
                               l.IsProcessed,
                          cancellationToken)
            : await _dbContext.WebhookAuditLogs
                .AnyAsync(l => l.OrderCode == payload.OrderCode &&
                               l.Status == payload.Status &&
                               l.EventTimeKey == eventTimeKey &&
                               l.IsProcessed,
                          cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Webhook event for OrderCode {OrderCode} with status {Status} already processed recently. Acknowledged.", payload.OrderCode, payload.Status);
            auditLog.IsProcessed = true;
            auditLog.ProcessMessage = "Duplicate webhook callback ignored (idempotent).";
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Duplicate webhook event acknowledged.", status = payload.Status });
        }

        var clientOrderGuid = Guid.TryParse(payload.ClientOrderCode, out var parsedGuid) ? parsedGuid : Guid.Empty;

        // 3. Find shipment by tracking number or order code
        var shipment = await _dbContext.Shipments
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.TrackingNumber == payload.OrderCode ||
                s.GhnOrderCode == payload.OrderCode ||
                (payload.ClientOrderCode != null && s.ClientOrderCode == payload.ClientOrderCode) ||
                (clientOrderGuid != Guid.Empty && s.OrderId == clientOrderGuid),
                cancellationToken);

        if (shipment == null)
        {
            _logger.LogWarning("Shipment with tracking {OrderCode} not found in database.", payload.OrderCode);
            auditLog.IsProcessed = false;
            auditLog.ProcessMessage = "Shipment not found in database.";
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Shipment not found, acknowledged." });
        }

        var isDeliveryFail = string.Equals(payload.Status?.Trim(), "delivery_fail", StringComparison.OrdinalIgnoreCase);
        var failedAttempts = shipment.Events.Count(e =>
            string.Equals(e.CarrierStatus, "delivery_fail", StringComparison.OrdinalIgnoreCase) ||
            e.Status == ShipmentEventStatus.DeliveryFailed) + 1;

        ShipmentStatus newStatus;
        if (isDeliveryFail && failedAttempts >= 3)
        {
            newStatus = ShipmentStatus.Returned;
            _logger.LogInformation("Shipment {TrackingNumber} reached 3 failed delivery attempts. Automatically transitioning to Returned/Returning.", shipment.TrackingNumber);
        }
        else
        {
            newStatus = GhnStatusMapper.Map(payload.Status);
        }

        var isRepeatedDeliveryAttempt = isDeliveryFail && failedAttempts < 3 && shipment.Status == ShipmentStatus.DeliveryFailed;

        // A late, repeated or unknown status must not undo progress (for example "in transit" after "delivered").
        if (!isRepeatedDeliveryAttempt && !ShipmentStatusTransitions.IsForward(shipment.Status, newStatus))
        {
            auditLog.IsProcessed = true;
            auditLog.ProcessMessage = $"Ignored out-of-order status '{newStatus}' (shipment is '{shipment.Status}').";
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Out-of-order webhook event ignored.", status = shipment.Status.ToString() });
        }

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
        var eventDescription = isDeliveryFail
            ? (failedAttempts >= 3
                ? $"Delivery attempt 3 failed. Package returning to sender. Reason: {payload.Reason ?? payload.Description ?? "Delivery failed"}."
                : $"Delivery attempt {failedAttempts} of 3 failed. Courier will reschedule. Reason: {payload.Reason ?? payload.Description ?? "Customer unavailable"}.")
            : (payload.Description ?? $"Status updated to {newStatus}");

        var shipmentEvent = new ShipmentEvent
        {
            ShipmentId = shipment.Id,
            CarrierStatus = payload.Status,
            Status = eventStatus,
            Description = eventDescription,
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

        // 5. Commerce must hear about the milestones that move an order. The update is queued on the audit row before it is sent, so a
        //    Commerce outage cannot lose it: the notification worker keeps retrying until Commerce acknowledges it.
        var notify = Array.IndexOf(NotifyStatuses, newStatus) >= 0 || (newStatus == ShipmentStatus.Returning && _behavior.EmitReturningStatus);
        if (notify)
        {
            auditLog.NotifyOrderId = shipment.OrderId;
            auditLog.NotifyStatus = MapCommerceStatus(newStatus);
            auditLog.NotifyTrackingNumber = shipment.TrackingNumber ?? payload.OrderCode;
            auditLog.NotifyDescription = payload.Description is { Length: > 500 } text ? text[..500] : payload.Description;
            auditLog.CommerceNotifyAttempts = 0;
            auditLog.CommerceNotifyNextRetryAt = DateTimeOffset.UtcNow.Add(DeliveryNotificationOutbox.FirstRetryDelay);
        }

        // Third failed attempt: send the parcel back (flag B). The attempts are counted from the saved events, plus this one.
        WebhookAuditLog? returningAudit = null;
        if (newStatus == ShipmentStatus.DeliveryFailed && _behavior.AutoReturnAfterFailedAttempts)
        {
            var persistedFailedAttempts = await _dbContext.ShipmentEvents
                .CountAsync(e => e.ShipmentId == shipment.Id && e.Status == ShipmentEventStatus.DeliveryFailed, cancellationToken) + 1;
            if (persistedFailedAttempts >= ShippingBehaviorOptions.MaxFailedAttempts)
            {
                shipment.Status = ShipmentStatus.Returning;
                _dbContext.ShipmentEvents.Add(new ShipmentEvent
                {
                    ShipmentId = shipment.Id,
                    CarrierStatus = "auto_returning",
                    Status = ShipmentEventStatus.Returning,
                    Description = $"Sent back automatically after {persistedFailedAttempts} failed delivery attempts.",
                    ExternalEventId = "auto_return",
                    OccurredAt = DateTimeOffset.UtcNow,
                    RawPayload = rawJson,
                    WarehouseName = payload.Warehouse
                });

                if (_behavior.EmitReturningStatus)
                {
                    returningAudit = new WebhookAuditLog
                    {
                        OrderCode = payload.OrderCode,
                        ClientOrderCode = payload.ClientOrderCode,
                        EventType = "auto_return",
                        Status = "returning",
                        RawJson = rawJson,
                        ReceivedAt = DateTimeOffset.UtcNow,
                        IsProcessed = true,
                        ProcessMessage = $"Moved to Returning after {persistedFailedAttempts} failed delivery attempts.",
                        NotifyOrderId = shipment.OrderId,
                        NotifyStatus = MapCommerceStatus(ShipmentStatus.Returning),
                        NotifyTrackingNumber = shipment.TrackingNumber ?? payload.OrderCode,
                        NotifyDescription = $"Sent back after {persistedFailedAttempts} failed delivery attempts.",
                        CommerceNotifyAttempts = 0,
                        CommerceNotifyNextRetryAt = DateTimeOffset.UtcNow.Add(DeliveryNotificationOutbox.FirstRetryDelay)
                    };
                    _dbContext.WebhookAuditLogs.Add(returningAudit);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (returningAudit is not null)
        {
            await _outbox.TrySendAsync(returningAudit, cancellationToken);
        }

        if (notify)
        {
            await _outbox.TrySendAsync(auditLog, cancellationToken);

            if (_notificationClient != null)
            {
                try
                {
                    var alertRequest = new DeliveryAlertRequest
                    {
                        OrderId = shipment.OrderId,
                        ShipmentId = shipment.Id,
                        ShopId = shipment.Warehouse?.SellerId ?? shipment.WarehouseId,
                        BuyerUserId = shipment.BuyerId ?? Guid.Empty,
                        SellerUserId = shipment.Warehouse?.SellerId ?? shipment.CreatedBy,
                        TrackingNumber = shipment.TrackingNumber ?? payload.OrderCode,
                        Status = MapCommerceStatus(newStatus),
                        Note = payload.Description ?? payload.Reason,
                        ShipperName = payload.ShipperName,
                        ShipperPhone = payload.ShipperPhone,
                        PodUrl = payload.PodUrl,
                        WarehouseName = payload.Warehouse ?? shipment.Warehouse?.Name,
                        OccurredAt = DateTimeOffset.UtcNow
                    };

                    await _notificationClient.SendDeliveryAlertAsync(alertRequest, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to dispatch delivery alert to NotificationService for Order {OrderId}", shipment.OrderId);
                }
            }
        }

        return Ok(new { message = "Webhook processed successfully.", status = newStatus.ToString() });
    }

    private static readonly ShipmentStatus[] NotifyStatuses =
    {
        ShipmentStatus.HandedOver,
        ShipmentStatus.InTransit,
        ShipmentStatus.Delivered,
        ShipmentStatus.DeliveryFailed,
        ShipmentStatus.Returned,
        ShipmentStatus.Cancelled
    };

    private static string MapCommerceStatus(ShipmentStatus status) => status switch
    {
        ShipmentStatus.DeliveryFailed => "DeliveryAttemptFailed",
        _ => status.ToString()
    };

    private static ShipmentEventStatus MapToEventStatus(ShipmentStatus status) => status switch
    {
        ShipmentStatus.ReadyToPick or ShipmentStatus.Packed => ShipmentEventStatus.Created,
        ShipmentStatus.Picking => ShipmentEventStatus.PickedUp,
        ShipmentStatus.InTransit or ShipmentStatus.HandedOver => ShipmentEventStatus.InTransit,
        ShipmentStatus.Delivered => ShipmentEventStatus.Delivered,
        ShipmentStatus.DeliveryFailed => ShipmentEventStatus.DeliveryFailed,
        ShipmentStatus.Returned => ShipmentEventStatus.Returned,
        ShipmentStatus.Returning => ShipmentEventStatus.Returning,
        ShipmentStatus.Cancelled => ShipmentEventStatus.Cancelled,
        _ => ShipmentEventStatus.InTransit
    };
}
