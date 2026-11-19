# WH-03: Lay Kho Cua Nguoi Ban Theo Seller ID (Get Warehouse By Seller ID)

## 1. Thong tin tong quan
- **Ma chuc nang**: `WH-03`
- **Ten API**: Get Warehouse By Seller ID
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses/seller/{sellerId}`
  - Chay Local: `http://localhost:8006/api/v1/warehouses/seller/{sellerId}`
- **Phan loai**: Public API
- **Quyen han / Role**: Tat ca nguoi dung (Public / Anonymous)

## 2. Mo ta nghiep vu
Tra cuu kho xuat hang dang hoat dong (`IsActive == true`) cua mot Nguoi ban cu the:
1. **Truy van kho cua Seller**: Tim kho hang theo `SellerId == sellerId && IsActive == true`.
2. **Xu ly khong tim thay**: Neu nguoi ban chua dang ky kho hoac kho dang bi tam khoa, he thong tra ve HTTP 404 Not Found kem ma loi `WAREHOUSE_NOT_FOUND`.

## 3. Path Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `sellerId` | `Guid` (UUID) | Co | Ma dinh danh nguoi ban |

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "id": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
  "code": "WH-SELLER-01",
  "name": "Kho Cua Hang Quan 1",
  "address": "So 123 Duong Nguyen Hue, Phuong Ben Nghe, Quan 1, TP. Ho Chi Minh",
  "contactPhone": "0901234567",
  "provinceName": "TP. Ho Chi Minh",
  "districtId": 1442,
  "wardCode": "20101",
  "ghnShopId": 5550559,
  "isDisabled": false,
  "sellerId": "b7e51c20-72e1-4bb2-901a-82672bfdbef6"
}
```

## 5. Phan hoi loi
- **404 Not Found (`WAREHOUSE_NOT_FOUND`)**: "No active warehouse found for seller '{sellerId}'."
- **500 Internal Server Error (`INTERNAL_SERVER_ERROR`)**: Loi he thong khong xac dinh.
