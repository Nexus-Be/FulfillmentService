# WH-01: Lấy Danh Sách Kho Đang Hoạt Động (List Active Warehouses)

## 1. Thông tin tổng quan
- **Mã chức năng**: `WH-01`
- **Tên API**: List Active Warehouses
- **Phương thức**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses`
  - Chạy Local: `http://localhost:5228/api/v1/warehouses`
- **Phân loại**: Public API
- **Quyền hạn / Role**: Tất cả người dùng (Public/Anonymous)

## 2. Mô tả nghiệp vụ
Truy vấn danh sách tất cả các kho bãi đang trong trạng thái hoạt động (`is_active = true`):
1. **Lọc dữ liệu**: Chỉ lấy các kho có `IsActive == true`. Các kho đã bị vô hiệu hóa (tạm khóa) sẽ không xuất hiện trong danh sách này.
2. **Sắp xếp**: Kết quả được sắp xếp tăng dần theo tên kho (`Name ASC`).
3. **Mục đích sử dụng**: 
   - Phục vụ Gateway/Frontend hiển thị danh sách các kho hàng khả dụng để điều phối đơn hàng hoặc gán vị trí lưu kho.

## 3. Headers
| Header | Giá trị | Bắt buộc |
|---|---|---|
| `Accept` | `application/json` | Không (Mặc định JSON) |

## 4. Query Parameters
*Không có query parameter bắt buộc.*

## 5. Phản hồi thành công (200 OK)
```json
[
  {
    "id": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
    "code": "WH-HCM-01",
    "name": "Kho Tổng Hồ Chí Minh",
    "address": "Khu Công Nghệ Cao, TP. Thủ Đức, TP. Hồ Chí Minh",
    "isActive": true,
    "sellerId": "00000000-0000-0000-0000-000000000000",
    "createdAt": "2026-10-31T08:00:00Z"
  },
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "code": "WH-HN-01",
    "name": "Kho Tổng Hà Nội",
    "address": "Khu Công Nghiệp Thăng Long, Đông Anh, Hà Nội",
    "isActive": true,
    "sellerId": null,
    "createdAt": "2026-10-31T08:30:00Z"
  }
]
```

## 6. Phản hồi lỗi
- **500 Internal Server Error (`INTERNAL_SERVER_ERROR`)**: Lỗi kết nối cơ sở dữ liệu PostgreSQL hoặc lỗi hệ thống không xác định.

