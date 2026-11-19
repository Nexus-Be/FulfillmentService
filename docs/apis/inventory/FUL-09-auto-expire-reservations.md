# FUL-09: Quet & Huy Phieu Giu Cho Qua Han (Auto Expire Reservations)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-09`
- **Ten API**: Auto Expire Reservations (Background Sweeper)
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/reservations/auto-expire`
  - Chay Local: `http://localhost:8006/api/v1/internal/inventories/reservations/auto-expire`
- **Phan loai**: Internal API (Goi boi background worker dinh ky moi phut)
- **Quyen han / Role**: System, Background Worker

## 2. Mo ta nghiep vu
Tu dong quet va don dep cac phieu giu cho ton kho da qua han ma khong thanh toan (`ExpiresAt <= UtcNow`):
1. **Nha hang ve ton kho kha dung**: Giam `ReservedQuantity` va tang lai `AvailableQuantity` tuong ung cho tat ca cac mat hang.
2. **Xoa phieu giu cho**: Xoa cac ban ghi het han khoi bang `InventoryReservations`.
3. **Ket qua**: Tra ve so luong phieu da duoc giai phong thanh cong.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Content-Type` | `application/json` | Co |

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "expiredCount": 5
}
```

## 5. Phan hoi loi
- **500 Internal Server Error (`INTERNAL_SERVER_ERROR`)**: Loi he thong khi quet don dep.
