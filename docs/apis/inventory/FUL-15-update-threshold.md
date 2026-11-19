# FUL-15: Cap Nhat Nguong Canh Bao Ton Kho Thap (Update Stock Threshold)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-15`
- **Ten API**: Update Stock Threshold
- **Phuong thuc**: `PUT`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/{skuId}/threshold`
  - Chay Local: `http://localhost:8006/api/v1/inventories/{skuId}/threshold`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin` (`[RequireRole("seller", "admin")]`)

## 2. Mo ta nghiep vu
Thiet lap nguong so luong an toan toi thieu (`minStockThreshold`) va bat/tat co canh bao ton kho thap (`isLowStockAlertEnabled`) cho mot ma SKU.
- Khi `AvailableQuantity <= minStockThreshold`, he thong se danh dau `isLowStock = true` de Seller chu dong nhap them hang.

## 3. Path Parameters & Request Body
```json
// Path: /api/v1/inventories/9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d/threshold
{
  "minStockThreshold": 20,
  "isLowStockAlertEnabled": true
}
```

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "id": "7ca92c12-3214-4112-a123-1c963f66bf11",
  "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
  "totalQuantity": 150,
  "reservedQuantity": 10,
  "availableQuantity": 140,
  "minStockThreshold": 20,
  "isLowStockAlertEnabled": true,
  "isLowStock": false,
  "updatedAt": "2026-11-19T09:00:00Z"
}
```

## 5. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: `minStockThreshold` vuot qua 1.000.000 hoac < 0.
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **404 Not Found (`INVENTORY_NOT_FOUND`)**: SKU khong ton tai.
