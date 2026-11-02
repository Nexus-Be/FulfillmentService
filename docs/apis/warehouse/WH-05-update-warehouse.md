# WH-05: Cập Nhật Thông Tin Kho (Update Warehouse)

## 1. Thông tin tổng quan
- **Mã chức năng**: `WH-05`
- **Tên API**: Update Warehouse
- **Phương thức**: `PUT`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses/{id}`
  - Chạy Local: `http://localhost:5228/api/v1/warehouses/{id}`
- **Phân loại**: Public API có xác thực quyền
- **Quyền hạn / Role**: `seller`, `admin` (Kiểm tra qua header `X-User-Roles`)

## 2. Mô tả nghiệp vụ
Cập nhật thông tin chi tiết (Tên và Địa chỉ) của kho bãi:
1. **Không cho phép sửa Mã kho (`Code`)**:
   - `Code` là mã định danh bất biến sau khi tạo để đảm bảo toàn vẹn dữ liệu cho các chứng từ vận đơn và tồn kho.
2. **Kiểm tra tồn tại**:
   - Nếu kho không tồn tại theo `id`, ném ngoại lệ `EntityNotFoundException` $\rightarrow$ Trả về mã HTTP 404 Not Found.
3. **Chuẩn hóa dữ liệu**:
   - Tên kho và địa chỉ được chuẩn hóa khoảng trắng tự động trước khi lưu vào cơ sở dữ liệu.

## 3. Headers
| Header | Giá trị | Bắt buộc | Mô tả |
|---|---|---|---|
| `Content-Type` | `application/json` | Có | Định dạng payload gửi lên |
| `X-User-Roles` | `seller` hoặc `admin` | Có | Role phân quyền người dùng từ API Gateway |

## 4. Path Parameters
| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `id` | `Guid` (UUID) | Có | Mã định danh kho cần cập nhật |

## 5. Request Body
```json
{
  "name": "Kho Quận 7 Mở Rộng",
  "address": "Số 458 Đường Nguyễn Thị Thập, Tân Phú, Quận 7, TP. Hồ Chí Minh"
}
```

### Chi tiết các trường:
| Trường | Kiểu | Bắt buộc | Ràng buộc | Mô tả |
|---|---|---|---|---|
| `name` | `string` | Có | 1 - 200 ký tự | Tên mới của kho bãi |
| `address` | `string?` | Không | Tối đa 500 ký tự | Địa chỉ mới của kho bãi |

## 6. Phản hồi thành công (200 OK)
```json
{
  "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "code": "WH-HCM-02",
  "name": "Kho Quận 7 Mở Rộng",
  "address": "Số 458 Đường Nguyễn Thị Thập, Tân Phú, Quận 7, TP. Hồ Chí Minh",
  "isActive": true,
  "sellerId": "b7e51c20-72e1-4bb2-901a-82672bfdbef6",
  "createdAt": "2026-11-02T06:00:00Z"
}
```

## 7. Phản hồi lỗi
- **400 Bad Request (`VAL_INVALID_INPUT` / `BUSINESS_RULE_VIOLATION`)**: Tên kho bị để trống hoặc vượt quá 200 ký tự.
- **403 Forbidden (`FORBIDDEN`)**: Request không có role `seller` hoặc `admin`.
- **404 Not Found (`NOT_FOUND`)**: Không tìm thấy kho với ID cung cấp.

