# FUL-06: Giu Cho Ton Kho Tam Thoi (Create Reservation)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-06`
- **Ten API**: Create Reservation (Hold Inventory)
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/reservations`
  - Chay Local: `http://localhost:8006/api/v1/internal/inventories/reservations`
- **Phan loai**: Internal API (OrderService / Commerce goi khi tao don hang)
- **Quyen han / Role**: Internal Service

## 2. Mo ta nghiep vu
Giu cho tam thoi so luong hang hoa trong 30 phut (`holdMinutes`) de cho nguoi mua thanh toan:
1. **Kiem tra ton kho tat ca cac mon (All-or-Nothing)**:
   - Quet qua toan bo SKU trong request.
   - Neu co bat ky SKU nao khong ton tai hoac so luong kha dung khong du (`availableQuantity < quantity`), he thong tra ve HTTP 400 Bad Request ngay lap tuc, khong giu cho do dang bat ky mon nao.
2. **Co che chong lap (Idempotency)**:
   - Neu `referenceId` da ton tai phieu giu cho dang hoat dong, tra ve thong tin phieu cu, khong cong don them.
3. **Khong ghi so cai**: Chi tang `ReservedQuantity`, khong ghi dong nao vao `InventoryLedgers` de tranh rac database.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Content-Type` | `application/json` | Co |

## 4. Request Body
```json
{
  "referenceId": "ORD-999",
  "holdMinutes": 30,
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "quantity": 2
    },
    {
      "skuId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 1
    }
  ]
}
```

## 5. Phan hoi thanh cong (200 OK)
```json
{
  "id": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
  "referenceId": "ORD-999",
  "expiresAt": "2026-11-01T10:30:00Z",
  "createdAt": "2026-11-01T10:00:00Z",
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "quantity": 2
    },
    {
      "skuId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 1
    }
  ]
}
```

## 6. Phan hoi loi
- **400 Bad Request (`INSUFFICIENT_INVENTORY`)**: Mot hoac nhieu SKU khong du ton kho kha dung de giu cho.
- **400 Bad Request (`VAL_INVALID_INPUT`)**: Danh sach items rong hoac `referenceId` thieu.
