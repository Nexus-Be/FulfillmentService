namespace Fulfillment.Application.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class DeliveryAlertRequest
{
    public Guid OrderId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? ShipperName { get; set; }
    public string? ShipperPhone { get; set; }
    public string? PodUrl { get; set; }
    public string? WarehouseName { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public interface INotificationClient
{
    Task<bool> SendShipmentStatusEmailAsync(
        string recipientEmail,
        string trackingNumber,
        string statusText,
        CancellationToken cancellationToken = default);

    Task<bool> SendDeliveryAlertAsync(
        DeliveryAlertRequest request,
        CancellationToken cancellationToken = default);
}
