# FUL-04: Lay Ton Kho Danh Sach SKU Hang Loat (Get Inventory By List SKU)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-04`
- **Ten API**: Get Inventory By List SKU (Batch Query)
- **Phuong thuc**: `POST`
- **URL**:
  - Cong khai: `POST /api/v1/inventories/by-skus`
  - Noi bo: `POST /api/v1/internal/inventories/batch-get`
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/by-skus`
  - Chay Local: `http://localhost:8006/api/v1/inventories/by-skus`
- **Phan loai**: Public API / Inter-Service
- **Quyen han / Role**: Tat ca nguoi dung (Public / Anonymous)

## 2. Mo ta nghiep vu
Truy van so du ton kho cua nhieu ma SKU cung luc trong mot request duy nhat, toi uu hoa cho Cart va Checkout nham tranh loi N+1 truy van mang.
- Neu SKU khong ton tai trong kho, he thong bo qua SKU do va chi tra ve danh sach nhung SKU tim thay.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Content-Type` | `application/json` | Co |

## 4. Request Body
```json
[
  "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "3fa85f64-5717-4562-b3fc-2c963f66afa6"
]
```

## 5. Phan hoi thanh cong (200 OK)
```json
[
  {
    "id": "7ca92c12-3214-4112-a123-1c963f66bf11",
    "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
    "totalQuantity": 100,
    "reservedQuantity": 15,
    "availableQuantity": 85,
    "soldQuantity": 10,
    "minStockThreshold": 10,
    "isLowStockAlertEnabled": true,
    "isLowStock": false,
    "lastMovementAt": "2026-11-19T08:00:00Z",
    "updatedAt": "2026-11-19T08:00:00Z"
  },
  {
    "id": "8ca92c12-3214-4112-a123-1c963f66bf22",
    "skuId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
    "totalQuantity": 50,
    "reservedQuantity": 0,
    "availableQuantity": 50,
    "soldQuantity": 5,
    "minStockThreshold": 10,
    "isLowStockAlertEnabled": true,
    "isLowStock": false,
    "lastMovementAt": "2026-11-19T08:00:00Z",
    "updatedAt": "2026-11-19T08:00:00Z"
  }
]
```

## 6. Phan hoi loi
- **500 Internal Server Error (`INTERNAL_SERVER_ERROR`)**: Loi he thong.
