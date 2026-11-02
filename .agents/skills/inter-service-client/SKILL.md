---
name: inter-service-client
description: Add or modify outbound typed REST clients in FulfillmentService (port 8006). Use when FulfillmentService updates order fulfillment status in CommerceService or dispatches delivery notifications via NotificationService.
---

# Outbound REST Client in FulfillmentService

> Scope: FulfillmentService repository only. Never reference parent workspace paths, root documentation, or sibling service directories.

## Local Facts

- Service: FulfillmentService
- Local HTTP Port: 8006
- Primary Downstream Targets:
  - NotificationService (`8007`): Shipment dispatched, out-for-delivery, and delivered alerts (`INotificationClient`)
  - CommerceService (`8003`): Updating order delivery milestone statuses (`ICommerceClient`)
- Interface Location: `Fulfillment.Application/Interfaces/`
- Implementation Location: `Fulfillment.Infrastructure/Services/{Target}/`
- DI Registration: `Fulfillment.Infrastructure/DependencyInjection.cs`

## Invariants

1. **100% RESTful HTTP/JSON:** No RabbitMQ, MassTransit, Saga, or Outbox. All cross-service calls use typed `HttpClient`.
2. **Strict Project Isolation:** Never reference external assemblies or query other microservice databases directly.
3. **No Internal API Key:** Inter-service routes use standard public endpoints. Do not send `X-Internal-Service-Key`.
4. **Idempotency on Status Transitions:** Milestone updates should pass an `Idempotency-Key` or tracking number in headers/payload to avoid redundant webhook triggers.
5. **Timeout:** Enforce `client.Timeout = TimeSpan.FromSeconds(10)`.
6. **W3C Distributed Tracing:** Transmit `traceparent` downstream when notifying CommerceService or NotificationService of shipment state transitions to maintain trace correlation in Tempo.

## Implementation Steps

### 1. Application Contract
Define `Fulfillment.Application/Interfaces/INotificationClient.cs`:
```csharp
namespace Fulfillment.Application.Interfaces;

public interface INotificationClient
{
    Task<bool> SendShipmentStatusEmailAsync(
        string recipientEmail,
        string trackingNumber,
        string statusText,
        CancellationToken cancellationToken = default);
}
```

### 2. Infrastructure Typed Client
Implement `Fulfillment.Infrastructure/Services/Notification/NotificationClient.cs`:
```csharp
namespace Fulfillment.Infrastructure.Services.Notification;

public sealed class NotificationClient : INotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationClient> _logger;

    public NotificationClient(HttpClient httpClient, ILogger<NotificationClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendShipmentStatusEmailAsync(
        string recipientEmail,
        string trackingNumber,
        string statusText,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                toEmail = recipientEmail,
                subject = $"Shipment Update: {trackingNumber}",
                htmlBody = $"<p>Your package {trackingNumber} is now <strong>{statusText}</strong>.</p>"
            };

            var response = await _httpClient.PostAsJsonAsync("api/v1/notifications/email/send", payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("[REST Notification] Failed shipment notification: {Status} {Error}", response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[REST Notification] Connection error while dispatching shipment email.");
            return false;
        }
    }
}
```

### 3. Dependency Injection Registration
In `Fulfillment.Infrastructure/DependencyInjection.cs`:
```csharp
var notificationBaseUrl = Environment.GetEnvironmentVariable("NOTIFICATION_SERVICE_URL")
    ?? configuration["NOTIFICATION_SERVICE_URL"]
    ?? configuration["Services:Notification:BaseUrl"]
    ?? "http://localhost:8007/";

services.AddHttpClient<INotificationClient, NotificationClient>(client =>
{
    client.BaseAddress = new Uri(notificationBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
```

### 4. Configuration Synchronization
Declare `NOTIFICATION_SERVICE_URL` and `COMMERCE_SERVICE_URL` in `docker-compose.yml` and `.env.example`.
