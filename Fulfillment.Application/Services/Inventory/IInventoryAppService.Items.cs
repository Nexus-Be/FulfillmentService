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
    /// [FUL-02]
    /// Adjusts inventory quantity by restocking or performing stock adjustments.
    /// Điều chỉnh số lượng tồn kho bằng cách nhập kho hoặc điều chỉnh kiểm kê.
    /// </summary>
    Task<AdjustInventoryResponse> AdjustInventoryAsync(AdjustInventoryRequest request, CancellationToken cancellationToken = default);

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
}
