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

    /// <summary>
    /// [FUL-05-ADV]
    /// Retrieves paginated and filtered inventory transaction ledgers with audit summary.
    /// Truy vấn danh sách sổ cái có phân trang, bộ lọc đa chiều và tóm tắt biến động.
    /// </summary>
    Task<InventoryLedgerPagedResponse> GetLedgersAsync(
        InventoryLedgerFilterRequest request,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-05-ADV]
    /// Exports filtered inventory transaction ledgers into an Excel spreadsheet.
    /// Xuất danh sách sổ cái kho ra file Excel .xlsx phục vụ kế toán và kiểm kê.
    /// </summary>
    Task<byte[]> ExportLedgersExcelAsync(
        InventoryLedgerFilterRequest request,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-05-ADV]
    /// Retrieves audit ledger entries linked to a specific business reference (e.g., Order, Auction).
    /// Tra cứu các bút toán sổ cái gắn liền với một chứng từ cụ thể (đơn hàng, phiên đấu giá).
    /// </summary>
    Task<IReadOnlyList<InventoryLedgerResponse>> GetLedgersByReferenceAsync(
        string referenceType,
        string referenceId,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);
}
