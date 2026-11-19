# FUL-03: Lay Thong Tin Ton Kho Theo 1 SKU (Get Inventory By SKU)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-03`
- **Ten API**: Get Inventory By SKU
- **Phuong thuc**: `GET`
- **URL**:
  - Cong khai: `GET /api/v1/inventories/{skuId}`
  - Noi bo: `GET /api/v1/internal/inventories/{skuId}`
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/{skuId}`
  - Chay Local: `http://localhost:8006/api/v1/inventories/{skuId}`
- **Phan loai**: Public API / Inter-Service
- **Quyen han / Role**: Tat ca nguoi dung (Public / Anonymous)

## 2. Mo ta nghiep vu
Tra cuu so du ton kho vat ly va cac chi so canh bao ton kho hien tai cua mot ma SKU bien the:
1. **Tinh toan ton kho kha dung**: `availableQuantity = totalQuantity - reservedQuantity`.
2. **Chi so canh bao ton kho thap**: `isLowStock = isLowStockAlertEnabled && availableQuantity <= minStockThreshold`.

## 3. Path Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `skuId` | `Guid` (UUID) | Co | Ma SKU san pham can kiem tra |

## 4. Phan hoi thanh cong (200 OK)
```json
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
```

## 5. Phan hoi loi
- **404 Not Found (`INVENTORY_NOT_FOUND`)**: SKU chua ton tai trong bat ky kho nao.
- **500 Internal Server Error (`INTERNAL_SERVER_ERROR`)**: Loi he thong.
