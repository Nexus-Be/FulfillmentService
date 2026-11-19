# FUL-16: Danh Sach Canh Bao Ton Kho Thap (Low Stock Alerts)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-16`
- **Ten API**: Get Low Stock Alerts
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/alerts/low-stock`
  - Chay Local: `http://localhost:8006/api/v1/inventories/alerts/low-stock`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin`, `staff` (`[RequireRole("seller", "admin", "staff")]`)

## 2. Mo ta nghiep vu
Truy van danh sach tat ca cac mat hang SKU dang co so luong kha dung cham hoac duoi nguong an toan (`availableQuantity <= minStockThreshold && isLowStockAlertEnabled == true`).
- Seller chi xem duoc canh bao cua kho minh; Admin/Staff xem toan san.

## 3. Query Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `warehouseId` | `Guid?` | Khong | Loc theo kho hang cu the |

## 4. Phan hoi thanh cong (200 OK)
```json
[
  {
    "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
    "availableQuantity": 5,
    "totalQuantity": 8,
    "reservedQuantity": 3,
    "minStockThreshold": 20,
    "lastMovementAt": "2026-11-19T08:00:00Z"
  }
]
```

## 5. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
