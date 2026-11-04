namespace Fulfillment.Application.Interfaces.Shipping;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface ICommerceWebhookClient
{
    Task NotifyDeliveryStatusAsync(
        Guid subOrderId,
        string status,
        string? trackingNumber = null,
        string? description = null,
        CancellationToken cancellationToken = default);
}

