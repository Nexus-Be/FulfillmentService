# FUL-19: Giai Phong Mot Phan Hang Giu Cho (Partial Release Reservation)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-19`
- **Ten API**: Release Reservation Items (Partial Release)
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/reservations/{referenceId}/release-items`
  - Chay Local: `http://localhost:8006/api/v1/internal/inventories/reservations/{referenceId}/release-items`
- **Phan loai**: Internal API (OrderService goi khi nguoi mua thay doi so luong hoac bo bot mon)
- **Quyen han / Role**: Internal Service

## 2. Mo ta nghiep vu
Giai phong mot phan so luong hang hoa cua mot so SKU cu the trong phieu giu cho:
- Nhan `subOrderId`, `reason` va danh sach `items` (`skuId`, `quantity`).
- Chi giam `ReservedQuantity` va tang lai `AvailableQuantity` cua dung cac mat hang duoc yeu cau.

## 3. Request Body
```json
{
  "subOrderId": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "reason": "Buyer reduced item quantity",
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "quantity": 1
    }
  ]
}
```

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "referenceId": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "status": "PARTIALLY_RELEASED",
  "releasedCount": 1
}
```
