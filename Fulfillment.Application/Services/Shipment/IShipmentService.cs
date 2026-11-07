namespace Fulfillment.Application.Services.Shipment;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Services.Shipping;

public interface IShipmentService
{
    /// <summary>
    /// Retrieves shipment details by unique tracking number.
    /// Lấy thông tin chi tiết vận đơn theo mã vận đơn vận chuyển.
    /// </summary>
    Task<ShipmentDto> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves shipment details associated with an order identifier.
    /// Lấy thông tin vận đơn gắn với mã định danh đơn hàng.
    /// </summary>
    Task<ShipmentDto> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates estimated shipping fee for a shop cart using carrier gateways with automatic timeout fallback.
    /// Tính toán phí vận chuyển dự kiến cho giỏ hàng của shop qua hãng vận chuyển, có cơ chế dự phòng khi quá thời gian chờ.
    /// </summary>
    Task<ShippingQuoteResponse> CalculateShippingFeeAsync(ShopShippingQuoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new physical shipment with carrier integration and sub-order idempotency.
    /// Khởi tạo vận đơn thực tế gửi sang hãng vận chuyển kèm cơ chế chống trùng lặp theo đơn hàng con (SubOrder).
    /// </summary>
    Task<CreateShipmentResponse> CreateShipmentAsync(CreateShipmentRequest request, CancellationToken cancellationToken = default);
}
