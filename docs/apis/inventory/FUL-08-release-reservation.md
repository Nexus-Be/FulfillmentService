# FUL-08: Huy Giu Cho Ton Kho (Release Reservation)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-08`
- **Ten API**: Release Reservation (Cancel Hold)
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/reservations/{referenceId}/release`
  - Chay Local: `http://localhost:8006/api/v1/internal/inventories/reservations/{referenceId}/release`
- **Phan loai**: Internal API (Goi khi huy don hang hoac thanh toan that bai)
- **Quyen han / Role**: Internal Service

## 2. Mo ta nghiep vu
Giai phong so luong hang dang bi giu cho tam thoi tra ve lai cho ton kho kha dung:
1. **Nha hang ve kho**: Giam `ReservedQuantity` va tang lai `AvailableQuantity`.
2. **Chan dung khi da Commit (Conflict Guard)**: Neu don hang da commit xuat kho truoc do (tim thay ban ghi `Commit` trong So cai), he thong chan dung thao tac huy va nem loi HTTP 409 `RESERVATION_ALREADY_COMMITTED`.
3. **Idempotency**: Goi lai nhieu lan cho don da huy tra ve thanh cong `RELEASED`, khong bao loi.

## 3. Path Parameters & Query Parameters
| Tham so | Vi tri | Kieu | Bat buoc | Mo ta |
|---|---|---|---|---|
| `referenceId` | Path | `string` | Co | Ma don hang tham chieu |
| `reason` | Query | `string?` | Khong | Ly do huy giu cho |

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "referenceId": "ORD-999",
  "status": "RELEASED",
  "releasedCount": 2
}
```

## 5. Phan hoi loi
- **409 Conflict (`RESERVATION_ALREADY_COMMITTED`)**: Don hang da commit xuat kho that su, khong the huy giu cho.
- **500 Internal Server Error (`INTERNAL_SERVER_ERROR`)**: Loi he thong.
