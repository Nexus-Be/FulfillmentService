# WH-03: Lấy Danh Sách Kho Của Người Bán (Get Warehouses By Seller ID)

## 1. Thông tin tổng quan
- **Mã chức năng**: `WH-03`
- **Tên API**: Get Warehouses By Seller ID
- **Phương thức**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses/seller/{sellerId}`
  - Chạy Local: `http://localhost:5228/api/v1/warehouses/seller/{sellerId}`
- **Phân loại**: Public API
- **Quyền hạn / Role**: Seller, Admin, Gateway Internal

## 2. Mô tả nghiệp vụ
Truy vấn danh sách tất cả các kho thuộc quyền sở hữu của một Người bán (`sellerId`):
1. **Lọc theo chủ shop**: Lọc tất cả các kho có `SellerId == sellerId` (bao gồm cả kho đang hoạt động và kho đang tạm khóa để chủ shop quản lý).
2. **Sắp xếp**: Kết quả được sắp xếp tăng dần theo tên kho (`Name ASC`).
3. **Danh sách rỗng**: Nếu người bán chưa đăng ký kho nào, trả về danh sách rỗng `[]` với mã HTTP 200 OK (không ném lỗi 404).

## 3. Path Parameters
| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `sellerId` | `Guid` (UUID) | Có | Mã định danh người bán (VD: `b7e51c20-72e1-4bb2-901a-82672bfdbef6`) |

## 4. Phản hồi thành công (200 OK)
```json
[
  {
    "id": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
    "code": "WH-SELLER-01",
    "name": "Kho Cửa Hàng Quận 1",
    "address": "Số 123 Đường Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
    "isActive": true,
    "sellerId": "b7e51c20-72e1-4bb2-901a-82672bfdbef6",
    "createdAt": "2026-10-31T08:00:00Z"
  }
]
```

## 5. Phản hồi lỗi
- **400 Bad Request**: Tham số `sellerId` không đúng định dạng GUID.

