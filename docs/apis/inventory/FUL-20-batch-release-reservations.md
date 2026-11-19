# FUL-20: Giai Phong Hang Loat Phieu Giu Cho (Batch Release Reservations)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-20`
- **Ten API**: Batch Release Reservations
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/reservations/batch-release`
  - Chay Local: `http://localhost:8006/api/v1/internal/inventories/reservations/batch-release`
- **Phan loai**: Internal API (Order Sweeper / Cancel Batch)
- **Quyen han / Role**: Internal Service

## 2. Mo ta nghiep vu
Giai phong hang loat cac phieu giu cho ton kho qua han hoac bi huy dong loat:
- Nhan mang `referenceIds` va ly do `reason`.
- Tra ve tong so luong phieu da huy thanh cong (`releasedCount`) va so luong that bai (`failedCount`).

## 3. Request Body
```json
{
  "referenceIds": [
    "ORD-101",
    "ORD-102"
  ],
  "reason": "Payment timeout batch sweep"
}
```

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "releasedCount": 2,
  "failedCount": 0
}
```
