namespace Fulfillment.Application.Services.Shipment;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Services.Shipping;

public interface IShipmentService
{
    Task<ShipmentDto> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default);
    Task<ShipmentDto> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<ShippingQuoteResponse> CalculateShippingFeeAsync(ShopShippingQuoteRequest request, CancellationToken cancellationToken = default);
    Task<CreateShipmentResponse> CreateShipmentAsync(CreateShipmentRequest request, CancellationToken cancellationToken = default);
}
