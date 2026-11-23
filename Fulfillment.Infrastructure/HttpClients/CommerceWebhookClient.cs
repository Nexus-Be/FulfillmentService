namespace Fulfillment.Infrastructure.HttpClients;

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces.Shipping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class CommerceWebhookClient : ICommerceWebhookClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CommerceWebhookClient> _logger;
    private readonly CommerceOptions _options;

    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public CommerceWebhookClient(
        IOptions<CommerceOptions> options,
        ILogger<CommerceWebhookClient> logger,
        HttpClient? httpClient = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new CommerceOptions();
        _httpClient = httpClient ?? new HttpClient();

        ConfigureClient();
    }

    /// <summary>
    /// Backwards-compatible constructor for testing and manual configuration.
    /// </summary>
    public CommerceWebhookClient(
        IConfiguration configuration,
        ILogger<CommerceWebhookClient> logger,
        HttpClient? httpClient = null)
        : this(Options.Create(BuildOptions(configuration)), logger, httpClient)
    {
    }

    private static CommerceOptions BuildOptions(IConfiguration configuration)
    {
        var options = new CommerceOptions();
        if (configuration == null)
            return options;

        var baseUrl = Environment.GetEnvironmentVariable("COMMERCE_SERVICE_URL")
            ?? configuration["COMMERCE_SERVICE_URL"]
            ?? configuration["Services:CommerceService:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
            options.BaseUrl = baseUrl;

        var path = configuration["Services:CommerceService:DeliveryStatusPath"]
            ?? configuration["COMMERCE_DELIVERY_STATUS_PATH"]
            ?? Environment.GetEnvironmentVariable("COMMERCE_DELIVERY_STATUS_PATH");
        if (!string.IsNullOrWhiteSpace(path))
            options.DeliveryStatusPath = path;

        var serviceKey = Environment.GetEnvironmentVariable("INTERNAL_SERVICE_SECRET")
            ?? Environment.GetEnvironmentVariable("INTERNAL_SERVICE_KEY")
            ?? configuration["INTERNAL_SERVICE_SECRET"]
            ?? configuration["INTERNAL_SERVICE_KEY"]
            ?? configuration["InternalSecurity:ServiceKey"];
        if (!string.IsNullOrWhiteSpace(serviceKey))
            options.ServiceKey = serviceKey;

        return options;
    }

    private void ConfigureClient()
    {
        if (_httpClient.BaseAddress == null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        }

        if (_options.TimeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        }
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
            var relativePath = _options.DeliveryStatusPath
                .Replace("{subOrderId}", request.SubOrderId.ToString(), StringComparison.OrdinalIgnoreCase)
                .TrimStart('/');

            using var requestMsg = new HttpRequestMessage(
                HttpMethod.Post,
                relativePath)
            {
                Content = JsonContent.Create(payload)
            };

            if (!requestMsg.Headers.Contains("X-Internal-Service-Key") && !string.IsNullOrWhiteSpace(_options.ServiceKey))
            {
                requestMsg.Headers.Add("X-Internal-Service-Key", _options.ServiceKey);
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
