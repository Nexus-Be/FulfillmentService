namespace Fulfillment.Application.Services.Warehouse;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IWarehouseService
{
    /// <summary>
    /// [FUL-??]
    /// Registers a new warehouse in the system.
    /// Đăng ký một kho hàng mới vào hệ thống.
    /// </summary>
    Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-??]
    /// Updates an existing warehouse's details.
    /// Cập nhật thông tin chi tiết của một kho hàng hiện có.
    /// </summary>
    Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-??]
    /// Retrieves warehouse details by unique identifier.
    /// Lấy thông tin chi tiết kho hàng theo mã định danh.
    /// </summary>
    Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-??]
    /// Updates the active status of a warehouse (active or deactivated).
    /// Cập nhật trạng thái hoạt động của kho hàng (kích hoạt hoặc tạm khóa).
    /// </summary>
    Task<WarehouseDto> SetActiveStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-??]
    /// Retrieves all warehouses belonging to a specific seller.
    /// Lấy danh sách tất cả các kho hàng thuộc về một chủ shop cụ thể.
    /// </summary>
    Task<IReadOnlyList<WarehouseDto>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FUL-??]
    /// Lists all active warehouses in the system.
    /// Lấy danh sách tất cả các kho hàng đang hoạt động trong hệ thống.
    /// </summary>
    Task<IReadOnlyList<WarehouseDto>> ListActiveAsync(CancellationToken cancellationToken = default);
}
