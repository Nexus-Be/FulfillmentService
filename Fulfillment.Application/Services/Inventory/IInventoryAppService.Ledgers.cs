namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public partial interface IInventoryAppService
{
    /// <summary>
    /// [FUL-05]
    /// Records an immutable audit ledger entry for stock balance change.
    /// Ghi nhận một bút toán lịch sử biến động tồn kho không thể sửa xóa.
    /// </summary>
    Task<InventoryLedgerResponse> RecordLedgerEntryAsync(RecordLedgerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-05]
    /// Retrieves ledger movement history for a specific SKU.
    /// Lấy lịch sử biến động sổ cái kho cho một SKU cụ thể.
    /// </summary>
    Task<IReadOnlyList<InventoryLedgerResponse>> GetLedgersBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default);
}
