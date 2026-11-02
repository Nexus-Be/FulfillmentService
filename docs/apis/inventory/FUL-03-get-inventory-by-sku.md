# FUL-03: Lấy Thông Tin Tồn Kho Theo 1 SKU (Get Inventory By SKU)

## 1. Thông tin tổng quan
- **Mã chức năng**: `FUL-03`
- **Tên API**: Get Inventory By SKU
- **Phương thức**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/{skuId}`
  - Chạy Local: `http://localhost:5228/api/v1/internal/inventories/{skuId}`
- **Phân loại**: Internal API (OrderService, CatalogService gọi tra cứu)
- **Quyền hạn / Role**: Buyer, Seller, Admin, Staff

## 2. Mô tả nghiệp vụ
Tra cứu số lượng tồn kho vật lý hiện tại của một mã SKU.
Luôn trả về 3 con số nhất quán:
- `totalQuantity`: Tổng hàng vật lý thực tế trong kho.
- `reservedQuantity`: Số lượng đang bị giữ chỗ tạm thời (khách đang chờ thanh toán).
- `availableQuantity`: Số lượng khả dụng có thể bán tiếp (`totalQuantity - reservedQuantity`).

## 3. Path Parameters
| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `skuId` | `Guid` | Có | Mã SKU sản phẩm cần kiểm tra |

## 4. Phản hồi thành công (200 OK)
```json
{
  "id": "7ca92c12-3214-4112-a123-1c963f66bf11",
  "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "warehouseId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "totalQuantity": 100,
  "reservedQuantity": 15,
  "availableQuantity": 85
}
```

## 5. Phản hồi lỗi
- **404 Not Found (`INVENTORY_NOT_FOUND`)**: SKU không tồn tại trong bất kỳ kho nào.

