# WH-06: Bật/Tắt Trạng Thái Hoạt Động Của Kho (Set Warehouse Active Status)

## 1. Thông tin tổng quan
- **Mã chức năng**: `WH-06`
- **Tên API**: Set Warehouse Active Status (Activate / Deactivate)
- **Phương thức**: `PATCH`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses/{id}/status?isActive={true|false}`
  - Chạy Local: `http://localhost:5228/api/v1/warehouses/{id}/status?isActive={true|false}`
- **Phân loại**: Public API có phân quyền chặt chẽ
- **Quyền hạn / Role**: `admin` (Duy nhất Admin hệ thống được phép thực hiện)

## 2. Mô tả nghiệp vụ
Bật hoặc tắt trạng thái hoạt động của kho bãi (tương đương cơ chế Tạm khóa / Mở khóa kho):
1. **Phân quyền đặc quyền (Admin Only)**:
   - Chỉ người dùng có role `admin` trong header `X-User-Roles` mới được phép đổi trạng thái hoạt động của kho.
2. **Ảnh hưởng khi vô hiệu hóa (`isActive = false`)**:
   - Kho sẽ bị ẩn khỏi danh sách `ListActiveAsync` (`WH-01`).
   - Các thao tác xuất/nhập/điều phối mới sẽ không được chọn kho này.
3. **Cập nhật trạng thái**:
   - Thay đổi giá trị cột `is_active` trong bảng `warehouses`.

## 3. Headers
| Header | Giá trị | Bắt buộc | Mô tả |
|---|---|---|---|
| `X-User-Roles` | `admin` | Có | Phải chứa role `admin` |

## 4. Path & Query Parameters
| Tham số | Vị trí | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|---|
| `id` | Path | `Guid` (UUID) | Có | Mã định danh kho bãi |
| `isActive` | Query | `bool` | Có | `true`: Kích hoạt kho, `false`: Tạm khóa kho |

## 5. Request Body
*Không có request body (sử dụng query parameter `isActive`).*

## 6. Phản hồi thành công (200 OK)
```json
{
  "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "code": "WH-HCM-02",
  "name": "Kho Quận 7 Mở Rộng",
  "address": "Số 458 Đường Nguyễn Thị Thập, Tân Phú, Quận 7, TP. Hồ Chí Minh",
  "isActive": false,
  "sellerId": "b7e51c20-72e1-4bb2-901a-82672bfdbef6",
  "createdAt": "2026-11-02T06:00:00Z"
}
```

## 7. Phản hồi lỗi
- **400 Bad Request**: Tham số `isActive` không phải kiểu boolean (`true`/`false`).
- **403 Forbidden (`FORBIDDEN`)**: Request không mang role `admin`.
- **404 Not Found (`NOT_FOUND`)**: Không tìm thấy kho với ID cung cấp.

