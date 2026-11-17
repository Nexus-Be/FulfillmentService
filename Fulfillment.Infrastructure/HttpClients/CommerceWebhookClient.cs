namespace Fulfillment.Infrastructure.HttpClients;

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces.Shipping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class CommerceWebhookClient : ICommerceWebhookClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CommerceWebhookClient> _logger;
    private readonly string _internalServiceKey;

    public CommerceWebhookClient(
        IConfiguration configuration,
        ILogger<CommerceWebhookClient> logger,
        HttpClient? httpClient = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? new HttpClient();

        var baseUrl = configuration["Services:CommerceService:BaseUrl"]
            ?? configuration["COMMERCE_SERVICE_URL"]
            ?? Environment.GetEnvironmentVariable("COMMERCE_SERVICE_URL")
            ?? "http://localhost:8003";

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }

        if (_httpClient.Timeout == TimeSpan.FromSeconds(100))
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        _internalServiceKey = configuration["InternalSecurity:ServiceKey"]
            ?? configuration["INTERNAL_SERVICE_SECRET"]
            ?? configuration["INTERNAL_SERVICE_KEY"]
            ?? Environment.GetEnvironmentVariable("INTERNAL_SERVICE_SECRET")
            ?? Environment.GetEnvironmentVariable("INTERNAL_SERVICE_KEY")
            ?? "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e";
    }

    public async Task<bool> NotifyDeliveryStatusAsync(
        CommerceDeliveryStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var payload = new
        {
            status = request.Status,
            carrierCode = string.IsNullOrWhiteSpace(request.CarrierCode) ? "GHN" : request.CarrierCode,
            trackingCode = request.TrackingCode,
            note = request.Note,
            deliveredAt = request.DeliveredAt ?? DateTimeOffset.UtcNow,
            location = request.Location,
            podUrl = request.PodUrl,
            shipperName = request.ShipperName,
            shipperPhone = request.ShipperPhone
        };

        try
        {
            using var requestMsg = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/v1/sub-orders/{request.SubOrderId}/delivery-status")
            {
                Content = JsonContent.Create(payload)
            };

            if (!requestMsg.Headers.Contains("X-Internal-Service-Key") && !string.IsNullOrWhiteSpace(_internalServiceKey))
            {
                requestMsg.Headers.Add("X-Internal-Service-Key", _internalServiceKey);
            }

            var response = await _httpClient.SendAsync(requestMsg, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Commerce delivery-status webhook failed HTTP {StatusCode}: {ErrorText} for SubOrder {SubOrderId}",
                    response.StatusCode,
                    errorText,
                    request.SubOrderId);
                return false;
            }

            _logger.LogInformation(
                "Successfully dispatched delivery status '{Status}' to CommerceService for SubOrder {SubOrderId}",
                request.Status,
                request.SubOrderId);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                ex,
                "Failed to send delivery-status to CommerceService for SubOrder {SubOrderId}",
                request.SubOrderId);
            return false;
        }
    }

    public Task<bool> NotifyDeliveryStatusAsync(
        Guid subOrderId,
        string status,
        string? trackingNumber = null,
        string? description = null,
        DateTimeOffset? occurredAt = null,
        CancellationToken cancellationToken = default)
    {
        var request = new CommerceDeliveryStatusRequest
        {
            SubOrderId = subOrderId,
            Status = status,
            CarrierCode = "GHN",
            TrackingCode = trackingNumber,
            Note = description,
            DeliveredAt = occurredAt ?? (string.Equals(status, "Delivered", StringComparison.OrdinalIgnoreCase)
                ? DateTimeOffset.UtcNow
                : null)
        };

        return NotifyDeliveryStatusAsync(request, cancellationToken);
    }
}
