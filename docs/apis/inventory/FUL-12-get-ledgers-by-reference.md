# FUL-12: Tra Cuu So Cai Theo Ma Chung Tu Tham Chieu (Get Ledgers By Reference)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-12`
- **Ten API**: Get Ledgers By Reference
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/ledgers/reference/{referenceType}/{referenceId}`
  - Chay Local: `http://localhost:8006/api/v1/inventories/ledgers/reference/{referenceType}/{referenceId}`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin`, `staff` (`[RequireRole("seller", "admin", "staff")]`)

## 2. Mo ta nghiep vu
Tra cuu cac dong nhat ky bien dong so cai ton kho duoc sinh ra tu mot ma chung tu nghiep vu cu the (Don hang Order, Phien dau gia Auction, Phieu nhap kho Manual Import):
- Giup doi soat chinh xac mot don hang da tru nhung mon nao, so luong bao nhieu, vao thoi diem nao.

## 3. Headers & Path Parameters
| Tham so | Vi tri | Kieu | Bat buoc | Mo ta |
|---|---|---|---|---|
| `referenceType` | Path | `string` | Co | Loai chung tu: `ORDER`, `AUCTION`, `IMPORT` |
| `referenceId` | Path | `string` | Co | Ma dinh danh chung tu (VD: `ORD-999`) |

## 4. Phan hoi thanh cong (200 OK)
```json
[
  {
    "id": "22222222-3333-4444-5555-666666666666",
    "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "movementType": "Commit",
    "quantity": 2,
    "reservationId": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
    "referenceType": "ORDER",
    "referenceId": "ORD-999",
    "note": "Xuat kho cho don hang ORD-999",
    "createdAt": "2026-11-01T10:15:00Z",
    "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
    "warehouseName": "Kho Tong Tan Binh"
  }
]
```

## 5. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Khong co quyen truy cap.
