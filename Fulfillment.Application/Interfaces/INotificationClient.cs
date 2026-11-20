namespace Fulfillment.Application.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class DeliveryAlertRequest
{
    public Guid OrderId { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid ShopId { get; set; }
    public Guid BuyerUserId { get; set; }
    public Guid SellerUserId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? ShipperName { get; set; }
    public string? ShipperPhone { get; set; }
    public string? PodUrl { get; set; }
    public string? WarehouseName { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed record ShipmentEventNotificationRequest(
    Guid SourceEventId,
    Guid ShipmentId,
    long SourceVersion,
    Guid OrderId,
    Guid ShopId,
    Guid BuyerUserId,
    Guid SellerUserId,
    string OrderKind,
    string OrderNo,
    string Carrier,
    string TrackingCode,
    string Status,
    DateTimeOffset PaidAt,
    DateTimeOffset SellerAcceptedAt,
    DateTimeOffset ShipmentCreatedAt,
    DateTimeOffset OccurredAt,
    string? Reason = null,
    string? TraceId = null);

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

    Task<bool> SendShipmentEventAsync(
        ShipmentEventNotificationRequest request,
        CancellationToken cancellationToken = default);
}
