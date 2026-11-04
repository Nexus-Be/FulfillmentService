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

    public CommerceWebhookClient(
        IConfiguration configuration,
        ILogger<CommerceWebhookClient> logger,
        HttpClient? httpClient = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? new HttpClient();

        var baseUrl = configuration["Services:CommerceService:BaseUrl"] ?? "http://localhost:8003";
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }
    }

    public async Task NotifyDeliveryStatusAsync(
        Guid subOrderId,
        string status,
        string? trackingNumber = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            subOrderId,
            status,
            trackingNumber,
            description,
            timestamp = DateTimeOffset.UtcNow
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/v1/sub-orders/{subOrderId}/delivery-status",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Commerce delivery-status webhook failed HTTP {StatusCode}: {ErrorText}", response.StatusCode, errorText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send delivery-status to CommerceService for SubOrder {SubOrderId}", subOrderId);
        }
    }
}

