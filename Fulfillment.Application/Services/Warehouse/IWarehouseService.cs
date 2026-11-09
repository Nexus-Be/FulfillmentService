namespace Fulfillment.Application.Services.Warehouse;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Common.Models;

public interface IWarehouseService
{
    /// <summary>
    /// [FUL-12]
    /// Registers a new warehouse for a seller, strictly enforcing a single warehouse per seller constraint.
    /// Đăng ký kho hàng mới cho người bán, ràng buộc nghiêm ngặt mỗi người bán chỉ được có một kho.
    /// </summary>
    Task<WarehouseDto> CreateAsync(CreateWarehouseRequest request, Guid sellerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-12]
    /// Automatically provisions a warehouse for a seller approved by UserService with idempotency.
    /// Tự động khởi tạo kho hàng khi UserService duyệt đơn mở shop của người bán kèm cơ chế chống trùng lặp.
    /// </summary>
    Task<InternalWarehouseResponse> CreateInternalAsync(InternalCreateWarehouseRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-12]
    /// Updates details of an existing warehouse.
    /// Cập nhật thông tin chi tiết một kho hàng hiện có.
    /// </summary>
    Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-12]
    /// Retrieves warehouse details by unique identifier.
    /// Lấy thông tin chi tiết một kho bãi theo mã định danh.
    /// </summary>
    Task<WarehouseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-12]
    /// Retrieves active warehouse details belonging to a specific seller.
    /// Lấy thông tin kho hàng đang hoạt động của một người bán cụ thể.
    /// </summary>
    Task<WarehouseDto?> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-12]
    /// Lists all active warehouses with database-level pagination.
    /// Lấy danh sách các kho hàng đang hoạt động kèm phân trang cơ sở dữ liệu.
    /// </summary>
    Task<PagedData<WarehouseDto>> ListActiveAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-12]
    /// Activates or deactivates a warehouse.
    /// Bật hoặc tắt trạng thái hoạt động của một kho hàng.
    /// </summary>
    Task<WarehouseDto> SetActiveStatusAsync(Guid id, bool isActive, Guid currentUserId = default, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-12]
    /// Retrieves or auto-provisions a single personal warehouse for a seller.
    /// Lấy hoặc tự động khởi tạo kho riêng duy nhất cho người bán.
    /// </summary>
    Task<WarehouseDto?> GetOrProvisionForSellerAsync(
        Guid userId,
        Guid? sellerProfileId = null,
        string? shopName = null,
        string? address = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-12]
    /// Updates warehouse name and pickup address by seller ID.
    /// Cập nhật tên kho và địa chỉ lấy hàng theo mã người bán.
    /// </summary>
    Task<WarehouseDto?> UpdateBySellerIdAsync(
        Guid sellerId,
        InternalUpdateSellerWarehouseRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-12]
    /// Deactivates or removes warehouse associated with a seller (used during rollback).
    /// Vô hiệu hóa hoặc xóa kho gắn với người bán (dùng khi rollback mở shop).
    /// </summary>
    Task<bool> DeleteBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);
}
