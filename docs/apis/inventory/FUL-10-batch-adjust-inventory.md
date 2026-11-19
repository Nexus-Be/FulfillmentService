# FUL-10: Nhap Kho & Dieu Chinh Ton Kho Hang Loat (Batch Adjust Inventory)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-10`
- **Ten API**: Batch Adjust Inventory
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/batch-adjust`
  - Chay Local: `http://localhost:8006/api/v1/inventories/batch-adjust`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin` (`[RequireRole("seller", "admin")]`)

## 2. Mo ta nghiep vu
Thuc hien nhap them hang hoac dieu chinh kiem ke so du ton kho cho nhieu SKU cung luc trong mot giao dich nguyen tu (Atomic transaction):
1. **Tinh nguyen tu**: Neu co bat ky SKU nao bi am kho (`TotalQuantity < ReservedQuantity`), toan bo lo hang se bi rollback, khong dieu chinh do dang.
2. **Ghi So cai**: Ghi dong tuong ung vao `InventoryLedgers` cho tung SKU kem theo ma `adjustmentLedgerId`.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Authorization` | `Bearer <JWT_TOKEN>` | Co |
| `Content-Type` | `application/json` | Co |

## 4. Request Body
```json
{
  "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
  "note": "Nhap kho lo hang moi",
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "type": "Intake",
      "quantity": 50,
      "note": "Nhap 50 ao thun"
    },
    {
      "skuId": "a2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e",
      "type": "Intake",
      "quantity": 30,
      "note": "Nhap 30 quan jean"
    }
  ]
}
```

## 5. Phan hoi thanh cong (200 OK)
```json
{
  "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
  "totalItemsAdjusted": 2,
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "totalQuantity": 150,
      "reservedQuantity": 10,
      "availableQuantity": 140,
      "adjustmentLedgerId": "f1a2b3c4-d5e6-4a7b-8c9d-0e1f2a3b4c5d"
    },
    {
      "skuId": "a2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e",
      "totalQuantity": 80,
      "reservedQuantity": 0,
      "availableQuantity": 80,
      "adjustmentLedgerId": "f2b3c4d5-e6a7-4b8c-9d0e-1f2a3b4c5d6e"
    }
  ]
}
```

## 6. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: Danh sach items rong.
- **400 Bad Request (`INVALID_ADJUSTMENT`)**: Lam am kho hang.
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Khong so huu kho hang.
