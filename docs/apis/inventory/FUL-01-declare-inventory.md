# FUL-01: Khai Báo Tồn Kho Ban Đầu (Declare Inventory)

## 1. Thông tin tổng quan
- **Mã chức năng**: `FUL-01`
- **Tên API**: Declare Inventory
- **Phương thức**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/declare`
  - Chạy Local: `http://localhost:5228/api/v1/internal/inventories/declare`
- **Phân loại**: Internal API (gọi từ Catalog / Seller Admin)
- **Quyền hạn / Role**: Seller, Admin

## 2. Mô tả nghiệp vụ
Khai báo số lượng tồn kho ban đầu cho một mã SKU tại một kho bãi vật lý cụ thể.
- Mỗi cặp `(skuId, warehouseId)` chỉ được phép khai báo 1 lần duy nhất (Unique Constraint).
- Khi khai báo thành công với số lượng > 0, hệ thống tự động ghi 1 dòng Sổ cái ban đầu (`INITIAL`).
- Công thức tồn kho: `AvailableQuantity = TotalQuantity - ReservedQuantity`.

## 3. Headers
| Header | Giá trị | Bắt buộc |
|---|---|---|
| `Content-Type` | `application/json` | Có |

## 4. Request Body
```json
{
  "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "warehouseId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "quantity": 100,
  "note": "Khởi tạo tồn kho ban đầu từ kho tổng"
}
```

### Chi tiết các trường:
| Trường | Kiểu dữ liệu | Bắt buộc | Mô tả |
|---|---|---|---|
| `skuId` | `Guid` | Có | Mã định danh SKU sản phẩm |
| `warehouseId` | `Guid` | Có | Mã kho bãi vật lý lưu trữ hàng |
| `quantity` | `int` | Có | Số lượng khởi tạo (phải $\ge 0$) |
| `note` | `string` | Không | Ghi chú nguồn gốc lô hàng |

## 5. Phản hồi thành công (200 OK)
```json
{
  "id": "7ca92c12-3214-4112-a123-1c963f66bf11",
  "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "warehouseId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "totalQuantity": 100,
  "reservedQuantity": 0,
  "availableQuantity": 100
}
```

## 6. Phản hồi lỗi
- **400 Bad Request (`BUSINESS_RULE_VIOLATION`)**: SKU đã được khai báo tồn kho tại kho bãi này từ trước.
- **400 Bad Request (`VAL_INVALID_ARGUMENT`)**: `quantity < 0` hoặc `skuId` rỗng.

