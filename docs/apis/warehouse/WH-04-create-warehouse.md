# WH-04: Đăng Ký Kho Hàng Mới (Create Warehouse)

## 1. Thông tin tổng quan
- **Mã chức năng**: `WH-04`
- **Tên API**: Create Warehouse
- **Phương thức**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses`
  - Chạy Local: `http://localhost:5228/api/v1/warehouses`
- **Phân loại**: Public API có xác thực quyền
- **Quyền hạn / Role**: `seller`, `admin` (Kiểm tra qua header `X-User-Roles`)

## 2. Mô tả nghiệp vụ
Đăng ký thiết lập một kho bãi mới trong hệ thống Fulfillment:
1. **Chuẩn hóa dữ liệu (Normalization)**:
   - `Code`: Tự động viết hoa toàn bộ và loại bỏ khoảng trắng thừa (`StringNormalizer.NormalizeCode`). Ví dụ: `" wh-hcm-01 "` $\rightarrow$ `"WH-HCM-01"`.
   - `Name`, `Address`: Chuẩn hóa khoảng trắng (`StringNormalizer.NormalizeTitle`).
2. **Quy tắc tính duy nhất (Unique Code)**:
   - Mã kho (`Code`) là duy nhất trong toàn hệ thống. Nếu mã kho đã tồn tại, cơ sở dữ liệu sẽ chặn và trả về lỗi xung đột.
3. **Mặc định trạng thái**:
   - Kho mới tạo luôn có `IsActive = true` (sẵn sàng nhập xuất hàng).
4. **Header Location (201 Created)**:
   - Khi tạo thành công, trả về HTTP 201 Created kèm Header `Location: /api/v1/warehouses/{id}`.

## 3. Headers
| Header | Giá trị | Bắt buộc | Mô tả |
|---|---|---|---|
| `Content-Type` | `application/json` | Có | Định dạng payload gửi lên |
| `X-User-Roles` | `seller` hoặc `admin` | Có | Role phân quyền người dùng từ API Gateway |

## 4. Request Body
```json
{
  "code": "WH-HCM-02",
  "name": "Kho Quận 7 Trung Tâm",
  "address": "Số 456 Đường Nguyễn Thị Thập, Tân Phú, Quận 7, TP. Hồ Chí Minh",
  "sellerId": "b7e51c20-72e1-4bb2-901a-82672bfdbef6"
}
```

### Chi tiết các trường:
| Trường | Kiểu | Bắt buộc | Ràng buộc | Mô tả |
|---|---|---|---|---|
| `code` | `string` | Có | 2 - 50 ký tự | Mã định danh kho (viết hoa, unique) |
| `name` | `string` | Có | Tối đa 200 ký tự | Tên hiển thị của kho bãi |
| `address` | `string?` | Không | Tối đa 500 ký tự | Địa chỉ vật lý của kho |
| `sellerId` | `Guid?` | Không | UUID | ID người bán sở hữu kho (để `null` nếu là kho tổng của sàn) |

## 5. Phản hồi thành công (201 Created)
- **Header**: `Location: /api/v1/warehouses/a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d`
- **Body**:
```json
{
  "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "code": "WH-HCM-02",
  "name": "Kho Quận 7 Trung Tâm",
  "address": "Số 456 Đường Nguyễn Thị Thập, Tân Phú, Quận 7, TP. Hồ Chí Minh",
  "isActive": true,
  "sellerId": "b7e51c20-72e1-4bb2-901a-82672bfdbef6",
  "createdAt": "2026-11-02T06:00:00Z"
}
```

## 6. Phản hồi lỗi
- **400 Bad Request (`VAL_INVALID_INPUT` / `BUSINESS_RULE_VIOLATION`)**:
  - Thiếu `code` hoặc `name`.
  - Độ dài `code` không nằm trong khoảng 2 - 50 ký tự.
- **403 Forbidden (`FORBIDDEN`)**:
  - Request không có role `seller` hoặc `admin` trong header `X-User-Roles`.
- **409 Conflict (`CONFLICT`)**:
  - Mã `code` đã được sử dụng bởi một kho khác.

