# FUL-18: Báo Cáo Phân Tích Tổng Quan Tồn Kho (Inventory Analytics)

## 1. Thông tin tổng quan
- **Mã chức năng**: `FUL-18`
- **Tên API**: Inventory Analytics Dashboard
- **Phương thức**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/analytics`
  - Chạy Local: `http://localhost:8006/api/v1/inventories/analytics`
- **Phân loại**: Public API
- **Quyền hạn / Role**: `seller`, `admin`, `staff` (`[RequireRole("seller", "admin", "staff")]`)

## 2. Mô tả nghiệp vụ
Cung cấp số liệu thống kê tổng hợp toàn diện về năng lực kho bãi và sức khỏe tồn hàng:
- `totalItems`: Tổng số chủng loại mã SKU trong kho.
- `totalStockQuantity`: Tổng số lượng hàng hóa vật lý.
- `totalAvailableQuantity`: Tổng số lượng sẵn sàng bán.
- `totalReservedQuantity`: Tổng số lượng đang giữ chỗ cho các đơn hàng.
- `totalSoldQuantity`: Tổng số lượng đã bán và xuất kho thành công.
- `lowStockCount`: Số lượng SKU đang dưới ngưỡng an toàn.
- `deadStockCount`: Số lượng SKU bị tồn đọng lâu ngày.
- Danh sách `items` chi tiết kèm hỗ trợ sắp xếp và phân trang.

## 3. Query Parameters
| Tham số | Kiểu | Mặc định | Mô tả |
|---|---|---|---|
| `warehouseId` | `Guid?` | `null` | Lọc theo kho |
| `sortBy` | `string?` | `null` | Sắp xếp theo cột (`availableQuantity`, `totalQuantity`...) |
| `sortDirection` | `string?` | `"desc"` | Hướng sắp xếp: `"asc"` hoặc `"desc"` |
| `page` | `int` | `1` | Số trang |
| `pageSize` | `int` | `20` | Kích thước trang |

## 4. Phản hồi thành công (200 OK)
```json
{
  "summary": {
    "totalItems": 150,
    "totalStockQuantity": 2500,
    "totalAvailableQuantity": 2350,
    "totalReservedQuantity": 150,
    "totalSoldQuantity": 1200,
    "lowStockCount": 5,
    "deadStockCount": 2
  },
  "items": {
    "items": [
      {
        "id": "7ca92c12-3214-4112-a123-1c963f66bf11",
        "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
        "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
        "totalQuantity": 150,
        "reservedQuantity": 10,
        "availableQuantity": 140,
        "soldQuantity": 25,
        "minStockThreshold": 10,
        "isLowStockAlertEnabled": true,
        "isLowStock": false,
        "lastMovementAt": "2026-11-19T08:00:00Z",
        "updatedAt": "2026-11-19T08:00:00Z"
      }
    ],
    "pagination": {
      "pageIndex": 1,
      "pageSize": 20,
      "totalCount": 150,
      "totalPages": 8,
      "hasPreviousPage": false,
      "hasNextPage": true
    }
  }
}
```

## 5. Phản hồi lỗi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thiếu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Không có quyền truy cập.
