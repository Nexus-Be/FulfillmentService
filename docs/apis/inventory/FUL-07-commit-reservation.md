# FUL-07: Chot Ban Hang & Xuat Kho That Su (Commit Reservation)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-07`
- **Ten API**: Commit Reservation (Confirm Order Outbound)
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/reservations/{referenceId}/commit`
  - Chay Local: `http://localhost:8006/api/v1/internal/inventories/reservations/{referenceId}/commit`
- **Phan loai**: Internal API (Goi khi don hang thanh toan thanh cong)
- **Quyen han / Role**: Internal Service

## 2. Mo ta nghiep vu
Chinh thuc chot don hang va xuat kho vat ly:
1. **Xuat kho vat ly**:
   - `TotalQuantity -= Quantity` (Hang that su xuat kho).
   - `ReservedQuantity -= Quantity` (Giai phong so luong tam giu).
   - `SoldQuantity += Quantity` (Tang so luong da ban).
2. **Ghi So cai ke toan**: Ghi 1 dong vao bang `InventoryLedgers` voi `MovementType = Commit` cho moi mat hang.
3. **Giai phong bang tam**: Xoa phieu giu cho khoi bang `InventoryReservations`.
4. **Bao ve Idempotency**: Neu goi lai, he thong tra cuu So cai da commit va tra ve thanh cong, khong tru kho lan 2.

## 3. Path Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `referenceId` | `string` | Co | Ma don hang tham chieu (VD: `ORD-999`) |

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "id": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
  "referenceId": "ORD-999",
  "expiresAt": "2026-11-01T10:15:00Z",
  "createdAt": "2026-11-01T10:15:00Z",
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "quantity": 2
    }
  ]
}
```

## 5. Phan hoi loi
- **404 Not Found (`NOT_FOUND`)**: Ma `referenceId` khong ton tai o ca phieu tam lan So cai.
- **500 Internal Server Error (`INTERNAL_SERVER_ERROR`)**: Loi he thong.
