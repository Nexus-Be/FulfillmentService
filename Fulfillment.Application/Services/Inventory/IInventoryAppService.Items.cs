namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public partial interface IInventoryAppService
{
    /// <summary>
    /// [FUL-01]
    /// Declares initial inventory for a specific SKU in a warehouse.
    /// Khai báo tồn kho ban đầu cho một SKU trong kho hàng.
    /// </summary>
    Task<InventoryItemResponse> DeclareInventoryAsync(DeclareInventoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-01]
    /// Declares initial inventory for a specific SKU with ownership validation.
    /// Khai báo tồn kho ban đầu cho một SKU kèm kiểm tra quyền sở hữu kho.
    /// </summary>
    Task<InventoryItemResponse> DeclareInventoryAsync(DeclareInventoryRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-02]
    /// Adjusts inventory quantity by restocking or performing stock adjustments.
    /// Điều chỉnh số lượng tồn kho bằng cách nhập kho hoặc điều chỉnh kiểm kê.
    /// </summary>
    Task<AdjustInventoryResponse> AdjustInventoryAsync(AdjustInventoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-02]
    /// Adjusts inventory quantity by restocking or performing stock adjustments with ownership validation.
    /// Điều chỉnh số lượng tồn kho bằng cách nhập kho hoặc điều chỉnh kiểm kê kèm kiểm tra quyền sở hữu kho.
    /// </summary>
    Task<AdjustInventoryResponse> AdjustInventoryAsync(AdjustInventoryRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-02B]
    /// Adjusts inventory quantities for a batch of SKUs atomically using delta (+/-) adjustments.
    /// Điều chỉnh số lượng tồn kho hàng loạt cho nhiều SKU trong cùng một transaction theo nguyên tắc cộng/trừ (+/-).
    /// </summary>
    Task<BatchAdjustInventoryResponse> BatchAdjustInventoryAsync(BatchAdjustInventoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-02B]
    /// Adjusts inventory quantities for a batch of SKUs atomically with warehouse ownership validation.
    /// Điều chỉnh số lượng tồn kho hàng loạt cho nhiều SKU kèm kiểm tra quyền sở hữu kho.
    /// </summary>
    Task<BatchAdjustInventoryResponse> BatchAdjustInventoryAsync(BatchAdjustInventoryRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-03]
    /// Retrieves inventory information for a specific SKU.
    /// Lấy thông tin tồn kho chi tiết cho một SKU cụ thể.
    /// </summary>
    Task<InventoryItemResponse?> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-04]
    /// Retrieves inventory information for a list of SKUs.
    /// Lấy thông tin tồn kho cho danh sách các SKU.
    /// </summary>
    Task<IReadOnlyList<InventoryItemResponse>> GetByListSkuAsync(IReadOnlyList<Guid> skuIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates low stock alert threshold and alert toggle for a specific SKU.
    /// </summary>
    Task<InventoryItemResponse> UpdateThresholdAsync(Guid skuId, UpdateStockThresholdRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all items currently below their low stock threshold.
    /// </summary>
    Task<IReadOnlyList<LowStockAlertItemResponse>> GetLowStockAlertsAsync(Guid? warehouseId, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves items with no movement/activity for the given number of days.
    /// </summary>
    Task<IReadOnlyList<DeadStockAlertItemResponse>> GetDeadStockAlertsAsync(Guid? warehouseId, int daysThreshold, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves aggregated inventory analytics and filtered items.
    /// </summary>
    Task<InventoryAnalyticsResponse> GetAnalyticsAsync(InventoryAnalyticsFilterRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes an uploaded Excel/CSV file with AI schema inference and returns a deterministic preview.
    /// </summary>
    Task<InventoryImportPreviewResponse> PreviewImportAsync(System.IO.Stream fileStream, string fileName, Guid? warehouseId, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);
}
