namespace Fulfillment.Application.Services.Shipment;

using System;
using System.Text.Json;
using Fulfillment.Application.Interfaces;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Delivers the shipment updates owed to CommerceService. The GHN webhook queues one on its audit row and tries it once;
/// whatever Commerce did not acknowledge is retried by <see cref="ProcessDueAsync"/> with a growing delay, so a Commerce outage
/// delays an order's delivery status instead of losing it.
/// </summary>
public sealed class DeliveryNotificationOutbox
{
    public const int MaxAttempts = 12;
    public static readonly TimeSpan FirstRetryDelay = TimeSpan.FromMinutes(1);

    // How long a row is reserved for the instance that took it, so two instances never send the same update at once.
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(2);

    private readonly IFulfillmentDbContext _dbContext;
    private readonly ICommerceWebhookClient _commerce;
    private readonly ILogger<DeliveryNotificationOutbox> _logger;

    public DeliveryNotificationOutbox(
        IFulfillmentDbContext dbContext, ICommerceWebhookClient commerce, ILogger<DeliveryNotificationOutbox> logger)
    {
        _dbContext = dbContext;
        _commerce = commerce;
        _logger = logger;
    }

    public static TimeSpan Backoff(int attempts) => attempts switch
    {
        <= 1 => TimeSpan.FromMinutes(1),
        2 => TimeSpan.FromMinutes(2),
        3 => TimeSpan.FromMinutes(5),
        4 => TimeSpan.FromMinutes(10),
        _ => TimeSpan.FromMinutes(30)
    };

    /// <summary>Sends one queued update and records the outcome. Returns true when Commerce acknowledged it.</summary>
    public async Task<bool> TrySendAsync(WebhookAuditLog log, CancellationToken cancellationToken = default)
    {
        if (log.NotifyOrderId is not { } orderId || string.IsNullOrWhiteSpace(log.NotifyStatus))
            return true;

        string? shipperName = null;
        string? shipperPhone = null;
        string? podUrl = null;
        string? location = null;
        string? reasonCode = null;
        string? reason = null;

        if (!string.IsNullOrWhiteSpace(log.RawJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(log.RawJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("ShipperName", out var sn) || root.TryGetProperty("shipperName", out sn))
                    shipperName = sn.GetString();
                if (root.TryGetProperty("ShipperPhone", out var sp) || root.TryGetProperty("shipperPhone", out sp))
                    shipperPhone = sp.GetString();
                if (root.TryGetProperty("POD", out var pod) || root.TryGetProperty("PodURL", out pod) || root.TryGetProperty("podUrl", out pod) || root.TryGetProperty("PodUrl", out pod))
                    podUrl = pod.GetString();
                if (root.TryGetProperty("Warehouse", out var wh) || root.TryGetProperty("warehouse", out wh) || root.TryGetProperty("location", out wh))
                    location = wh.GetString();
                // The reason is read from the stored carrier payload, so a retry sends exactly what the carrier reported the first time.
                if (root.TryGetProperty("ReasonCode", out var rc) || root.TryGetProperty("reasonCode", out rc))
                    reasonCode = rc.ValueKind == JsonValueKind.Null ? null : rc.ToString();
                if (root.TryGetProperty("Reason", out var rs) || root.TryGetProperty("reason", out rs))
                    reason = rs.ValueKind == JsonValueKind.Null ? null : rs.ToString();
            }
            catch
            {
                // Fall back to null fields if raw JSON cannot be parsed
            }
        }

        var request = new CommerceDeliveryStatusRequest
        {
            SubOrderId = orderId,
            Status = log.NotifyStatus,
            CarrierCode = "GHN",
            TrackingCode = log.NotifyTrackingNumber,
            Note = log.NotifyDescription,
            DeliveredAt = string.Equals(log.NotifyStatus, "Delivered", StringComparison.OrdinalIgnoreCase)
                ? (log.ReceivedAt != default ? log.ReceivedAt : DateTimeOffset.UtcNow)
                : log.ReceivedAt,
            Location = location,
            PodUrl = podUrl,
            ShipperName = shipperName,
            ShipperPhone = shipperPhone,
            ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? null : reasonCode.Trim(),
            Reason = string.IsNullOrWhiteSpace(reason) ? null : (reason.Length > 500 ? reason[..500] : reason)
        };

        bool delivered;
        try
        {
            delivered = await _commerce.NotifyDeliveryStatusAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Sending the {Status} update of order {OrderId} to Commerce threw.", log.NotifyStatus, orderId);
            delivered = false;
        }

        var now = DateTimeOffset.UtcNow;
        if (delivered)
        {
            log.CommerceNotifiedAt = now;
            log.CommerceNotifyNextRetryAt = null;
        }
        else
        {
            log.CommerceNotifyAttempts += 1;
            log.CommerceNotifyNextRetryAt = now.Add(Backoff(log.CommerceNotifyAttempts));
            if (log.CommerceNotifyAttempts >= MaxAttempts)
            {
                _logger.LogError(
                    "The {Status} update of order {OrderId} could not be delivered to Commerce after {Attempts} attempts. It needs manual attention.",
                    log.NotifyStatus, orderId, log.CommerceNotifyAttempts);
            }
        }

        log.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return delivered;
    }

    /// <summary>Retries every update whose time has come. Returns how many this instance attempted.</summary>
    public async Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var due = await _dbContext.WebhookAuditLogs
            .Where(l => !l.IsDeleted
                        && l.NotifyOrderId != null
                        && l.CommerceNotifiedAt == null
                        && l.CommerceNotifyAttempts < MaxAttempts
                        && (l.CommerceNotifyNextRetryAt == null || l.CommerceNotifyNextRetryAt <= now))
            .OrderBy(l => l.ReceivedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var attempted = 0;
        foreach (var log in due)
        {
            if (!await TryClaimAsync(log, now, cancellationToken))
                continue;

            await TrySendAsync(log, cancellationToken);
            attempted++;
        }

        return attempted;
    }

    /// <summary>Reserves the row with a conditional UPDATE that succeeds for only one instance. Providers without set-based updates run one process.</summary>
    private async Task<bool> TryClaimAsync(WebhookAuditLog log, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var observedNext = log.CommerceNotifyNextRetryAt;
        var observedAttempts = log.CommerceNotifyAttempts;
        DateTimeOffset? leaseUntil = now.Add(ClaimLease);

        if (_dbContext is DbContext context && context.Database.IsRelational())
        {
            var updated = await _dbContext.WebhookAuditLogs
                .Where(l => l.Id == log.Id
                            && l.CommerceNotifiedAt == null
                            && l.CommerceNotifyAttempts == observedAttempts
                            && l.CommerceNotifyNextRetryAt == observedNext)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.CommerceNotifyNextRetryAt, leaseUntil), cancellationToken);
            if (updated == 0)
                return false;
        }

        log.CommerceNotifyNextRetryAt = leaseUntil;
        return true;
    }
}
