namespace Fulfillment.Application.Services.Shipment;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Services.Shipping;

public interface IShipmentService
{
    /// <summary>
    /// Lists shipments with filtering and pagination, enforcing seller-level isolation.
    /// Lấy danh sách vận đơn có lọc và phân trang, đảm bảo bảo vệ dữ liệu giữa các người bán.
    /// </summary>
    Task<PagedData<ShipmentDto>> ListShipmentsAsync(ShipmentFilterRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a shipment synchronously with the shipping carrier.
    /// Hủy vận đơn và đồng bộ trực tiếp sang hãng vận chuyển (GHN).
    /// </summary>
    Task<CancelShipmentResponse> CancelShipmentAsync(Guid shipmentId, string? reason, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates or retrieves shipping label print token and formatted URL for thermal printing.
    /// Tạo hoặc lấy mã in tem vận chuyển kèm đường dẫn in phiếu gửi hàng GHN (A5, 80x80, 52x70).
    /// </summary>
    Task<PrintShippingLabelResponse> GetPrintLabelAsync(Guid shipmentId, string pageSize, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// [P2-07]
    /// Seller confirms received returned parcel and automatically restores inventory stock.
    /// Người bán xác nhận nhận lại kiện hàng chuyển hoàn và tự động hoàn trả số lượng vào kho.
    /// </summary>
    /// <remarks>
    /// Visual Flowchart: <see href="file:///d:/FSA/FulfillmentService/docs/diagrams/confirm_return_flow.svg">View Flowchart Diagram</see>
    /// </remarks>
    Task<ConfirmReturnResponse> ConfirmReturnAsync(Guid orderId, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Simulates courier/shipper status milestone updates for sandbox testing.
    /// Updates shipment lifecycle, logs shipment events and audit logs, and dispatches updates to Commerce and Notification services.
    /// </summary>
    Task<SimulateShipperStatusResponse> SimulateShipperStatusAsync(
        SimulateShipperStatusRequest request,
        string? trackingNumber = null,
        CancellationToken cancellationToken = default);
}
