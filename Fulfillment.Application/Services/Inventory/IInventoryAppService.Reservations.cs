namespace Fulfillment.Application.Services.Inventory;

using System.Threading;
using System.Threading.Tasks;

public partial interface IInventoryAppService
{
    /// <summary>
    /// [FUL-08]
    /// Reserves inventory items for a reference (order, hold), incrementing reserved_quantity.
    /// Giữ chỗ số lượng tồn kho theo đơn, tăng reserved_quantity và tạo thẻ giữ chỗ ACTIVE.
    /// </summary>
    Task<InventoryReservationResponse> CreateReservationAsync(CreateReservationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-09]
    /// Commits a reservation upon successful payment, permanently reducing total and reserved stock.
    /// Xác nhận thanh toán thành công, trừ vĩnh viễn tồn kho và chuyển thẻ giữ chỗ sang CONFIRMED.
    /// </summary>
    Task<InventoryReservationResponse> CommitReservationAsync(string referenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-10]
    /// Releases a reservation upon cancellation or failure, restoring available stock.
    /// Hủy thẻ giữ chỗ và hoàn trả số lượng reserved_quantity về lại tồn kho khả dụng.
    /// </summary>
    Task<InventoryReservationResponse> ReleaseReservationAsync(string referenceId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-11]
    /// Scans and automatically releases expired reservations.
    /// Quét định kỳ nhả các thẻ giữ chỗ quá hạn về lại tồn kho khả dụng.
    /// </summary>
    Task<int> AutoExpireReservationsAsync(CancellationToken cancellationToken = default);
}
