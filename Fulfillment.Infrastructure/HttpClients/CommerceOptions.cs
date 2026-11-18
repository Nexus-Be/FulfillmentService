namespace Fulfillment.Infrastructure.HttpClients;

/// <summary>
/// Centralized configuration for communicating with CommerceService.
/// Controls the base address, endpoint paths, and authentication credentials.
/// </summary>
public sealed class CommerceOptions
{
    public const string SectionName = "Services:CommerceService";

    /// <summary>Base URL of CommerceService (e.g. http://localhost:8003 or https://commerce-service.onrender.com).</summary>
    public string BaseUrl { get; set; } = "http://localhost:8003";

    /// <summary>Relative endpoint template for notifying delivery status. Supports {subOrderId} placeholder.</summary>
    public string DeliveryStatusPath { get; set; } = "api/v1/sub-orders/{subOrderId}/delivery-status";

    /// <summary>Internal service-to-service key (X-Internal-Service-Key).</summary>
    public string ServiceKey { get; set; } = "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e";

    /// <summary>Request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 10;
}

