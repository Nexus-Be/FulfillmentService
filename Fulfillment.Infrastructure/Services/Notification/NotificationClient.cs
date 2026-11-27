namespace Fulfillment.Infrastructure.Services.Notification;

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public sealed class NotificationClient : INotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationClient> _logger;
    private readonly string? _internalServiceKey;

    public NotificationClient(
        HttpClient httpClient,
        ILogger<NotificationClient> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _internalServiceKey = configuration["InternalSecurity:ServiceKey"]
            ?? configuration["INTERNAL_SERVICE_KEY"]
            ?? Environment.GetEnvironmentVariable("INTERNAL_SERVICE_KEY");
    }

    public async Task<bool> SendShipmentStatusEmailAsync(
        string recipientEmail,
        string trackingNumber,
        string statusText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogWarning("[REST Notification] Recipient email is empty, skipping status email.");
            return false;
        }

        try
        {
            var payload = new
            {
                toEmail = recipientEmail,
                subject = $"Shipment Update: {trackingNumber}",
                htmlBody = $"<p>Your package <strong>{trackingNumber}</strong> is now <strong>{statusText}</strong>.</p>"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/notifications/email/send")
            {
                Content = JsonContent.Create(payload)
            };

            request.Headers.Add("x-user-id", "00000000-0000-0000-0000-000000000006");
            request.Headers.Add("x-user-roles", "service");
            request.Headers.Add("X-Caller-Service", "fulfillment-service");

            if (!string.IsNullOrWhiteSpace(_internalServiceKey))
            {
                request.Headers.Add("X-Internal-Service-Key", _internalServiceKey);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[REST Notification] Successfully sent shipment email for {TrackingNumber} to {Email}", trackingNumber, recipientEmail);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("[REST Notification] Failed shipment notification: HTTP {Status} {Error}", response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[REST Notification] Connection error while dispatching shipment email for {TrackingNumber}", trackingNumber);
            return false;
        }
    }

    public async Task<bool> SendDeliveryAlertAsync(
        DeliveryAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return false;
        }

        var recipients = new System.Collections.Generic.HashSet<Guid>();
        if (request.BuyerUserId != Guid.Empty) recipients.Add(request.BuyerUserId);
        if (request.SellerUserId != Guid.Empty) recipients.Add(request.SellerUserId);

        if (recipients.Count == 0)
        {
            _logger.LogWarning(
                "[REST Notification] Skipping delivery alert for Order {OrderId}: no buyer or seller user ID.",
                request.OrderId);
            return false;
        }

        try
        {
            var metadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                orderId = request.OrderId,
                shipmentId = request.ShipmentId,
                trackingNumber = request.TrackingNumber,
                status = request.Status,
                note = request.Note,
                shipperName = request.ShipperName,
                shipperPhone = request.ShipperPhone,
                podUrl = request.PodUrl,
                warehouseName = request.WarehouseName,
                occurredAt = request.OccurredAt
            });

            var notificationType = string.Equals(request.Status, "Delivered", StringComparison.OrdinalIgnoreCase)
                ? "OrderCompleted"
                : "OrderShipped";

            var title = $"Shipment update: {request.Status}";
            var body = string.IsNullOrWhiteSpace(request.Note)
                ? $"Shipment {request.TrackingNumber} for order {request.OrderId} is now {request.Status}."
                : $"Shipment {request.TrackingNumber} is now {request.Status}: {request.Note}";

            var allSucceeded = true;
            foreach (var userId in recipients)
            {
                var payload = new
                {
                    userId,
                    title,
                    body,
                    notificationType,
                    payload = metadataJson
                };

                using var requestMsg = new HttpRequestMessage(HttpMethod.Post, "api/v1/notifications/in-app")
                {
                    Content = JsonContent.Create(payload)
                };

                if (!string.IsNullOrWhiteSpace(_internalServiceKey))
                {
                    requestMsg.Headers.Add("X-Internal-Service-Key", _internalServiceKey);
                }

                var response = await _httpClient.SendAsync(requestMsg, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "[REST Notification] Dispatched delivery alert to user {UserId} for Order {OrderId}, Status {Status}",
                        userId,
                        request.OrderId,
                        request.Status);
                }
                else
                {
                    allSucceeded = false;
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "[REST Notification] NotificationService returned HTTP {StatusCode}: {Error} for Order {OrderId}",
                        response.StatusCode,
                        error,
                        request.OrderId);
                }
            }

            return allSucceeded;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[REST Notification] Connection error while dispatching delivery alert to NotificationService for Order {OrderId}",
                request.OrderId);
            return false;
        }
    }

    public async Task<bool> SendShipmentEventAsync(
        ShipmentEventNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return false;
        }

        try
        {
            var payload = new
            {
                sourceEventId = request.SourceEventId,
                shipmentId = request.ShipmentId,
                sourceVersion = request.SourceVersion,
                orderId = request.OrderId,
                shopId = request.ShopId,
                buyerUserId = request.BuyerUserId,
                sellerUserId = request.SellerUserId,
                orderKind = request.OrderKind,
                orderNo = request.OrderNo,
                carrier = request.Carrier,
                trackingCode = request.TrackingCode,
                status = MapCanonicalStatus(request.Status),
                paidAt = request.PaidAt,
                sellerAcceptedAt = request.SellerAcceptedAt,
                shipmentCreatedAt = request.ShipmentCreatedAt,
                occurredAt = request.OccurredAt,
                reason = request.Reason,
                traceId = request.TraceId
            };

            using var requestMsg = new HttpRequestMessage(HttpMethod.Post, "api/v1/notifications/shipment-events")
            {
                Content = JsonContent.Create(payload)
            };

            if (!string.IsNullOrWhiteSpace(_internalServiceKey))
            {
                requestMsg.Headers.Add("X-Internal-Service-Key", _internalServiceKey);
            }
            requestMsg.Headers.Add("x-user-roles", "service");
            requestMsg.Headers.Add("x-user-id", Guid.Empty.ToString());

            var response = await _httpClient.SendAsync(requestMsg, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "[REST Notification] Successfully sent shipment-event to NotificationService for Order {OrderId}, Status {Status}",
                    request.OrderId,
                    request.Status);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "[REST Notification] Notification shipment-events failed HTTP {StatusCode}: {Error} for Order {OrderId}",
                response.StatusCode,
                error,
                request.OrderId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[REST Notification] Connection error while sending shipment-event to NotificationService for Order {OrderId}",
                request.OrderId);
            return false;
        }
    }

    private static string MapCanonicalStatus(string status) => status?.Trim().ToLowerInvariant() switch
    {
        "created" or "readytopick" or "packed" => "Created",
        "picking" or "picked" or "pickedup" or "handedover" => "PickedUp",
        "intransit" or "delivering" or "shipped" => "InTransit",
        "outfordelivery" => "OutForDelivery",
        "delivered" => "Delivered",
        "deliveryfailed" or "deliveryattemptfailed" or "delivery_fail" => "DeliveryFailed",
        "returning" or "return" => "Returning",
        "returned" or "return_to_origin" => "Returned",
        "cancelled" or "cancel" => "Cancelled",
        _ => "InTransit"
    };
}
