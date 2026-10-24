namespace Fulfillment.Application.Services.Shipment;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IShipmentService
{
    Task<ShipmentDto?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default);
    Task<ShipmentDto?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
