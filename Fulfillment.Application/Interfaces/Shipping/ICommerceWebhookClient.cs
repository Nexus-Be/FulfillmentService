namespace Fulfillment.Application.Interfaces.Shipping;

using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class CommerceDeliveryStatusRequest
{
    public Guid SubOrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CarrierCode { get; set; } = "GHN";
    public string? TrackingCode { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? Location { get; set; }
    public string? PodUrl { get; set; }
    public string? ShipperName { get; set; }
    public string? ShipperPhone { get; set; }
}

public interface ICommerceWebhookClient
{
    /// <summary>
    /// Tells Commerce the delivery status of an order. Returns true only when Commerce acknowledged it (2xx), so the caller knows
    /// whether the update still has to be retried. <paramref name="occurredAt"/> is when the carrier reported it, not when it is sent.
    /// </summary>
    Task<bool> NotifyDeliveryStatusAsync(
        Guid subOrderId,
        string status,
        string? trackingNumber = null,
        string? description = null,
        DateTimeOffset? occurredAt = null,
        CancellationToken cancellationToken = default);

    Task<bool> NotifyDeliveryStatusAsync(
        CommerceDeliveryStatusRequest request,
        CancellationToken cancellationToken = default) =>
        NotifyDeliveryStatusAsync(
            request.SubOrderId,
            request.Status,
            request.TrackingCode,
            request.Note,
            request.DeliveredAt,
            cancellationToken);
}
