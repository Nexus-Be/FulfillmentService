# WH-04: Dang Ky Kho Hang Moi (Create Warehouse)

## 1. Thong tin tong quan
- **Ma chuc nang**: `WH-04`
- **Ten API**: Create Warehouse
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses`
  - Chay Local: `http://localhost:8006/api/v1/warehouses`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin` (`[RequireRole("seller", "admin")]`)

## 2. Mo ta nghiep vu
Thiet lap kho bai vat ly moi tren he thong:
1. **Dinh danh va Rang buoc**:
   - `code`: Ma dinh danh duy nhat toan he thong, tu dong viet hoa (`WH-SELLER-02`).
   - `name`: Ten hien thi kho.
   - `contactPhone`: So dien thoai kho xuat hang dung cho buu ta lien he lay hang.
   - `districtId`, `wardCode`, `provinceName`: Thong tin dia danh chuan GHN phuc vu lay hang va tinh cuoc.
   - `provinceId`, `districtId`, `wardCode` (24/11/2026): neu co bat ky gia tri nao thi phai co du ca ba va duoc doi chieu voi danh sach GHN truoc khi luu. Sai bo ma: `400 VAL_INVALID_LOCATION`; thieu mot phan: `400 WAREHOUSE_ADDRESS_INCOMPLETE`; khong co nguon GHN dang tin cay: `503 LOCATION_SOURCE_UNAVAILABLE` va khong luu. Kho khong co dia chi nao la kho giu cho (placeholder): luu duoc nhung khong bao gia duoc va khong tao van don duoc (`400 WAREHOUSE_ADDRESS_INCOMPLETE`) cho den khi hoan thien. Phan hoi tra ca `provinceId`.
   - `ghnShopId`: Ma cua hang tuong ung tren cong GHN.
2. **Quyen so huu**: Seller tao kho se tu dong gan `SellerId = currentUserId`.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Authorization` | `Bearer <JWT_TOKEN>` | Co |
| `Content-Type` | `application/json` | Co |

## 4. Request Body
```json
{
  "code": "WH-SELLER-02",
  "name": "Kho Quan 7 Trung Tam",
  "address": "So 456 Duong Nguyen Thi Thap, Tan Phu, Quan 7, TP. Ho Chi Minh",
  "contactPhone": "0909888999",
  "provinceName": "TP. Ho Chi Minh",
  "provinceId": 202,
  "districtId": 1450,
  "wardCode": "20804",
  "ghnShopId": 5550559,
  "sellerId": "b7e51c20-72e1-4bb2-901a-82672bfdbef6"
}
```

## 5. Phan hoi thanh cong (201 Created)
```json
{
  "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "code": "WH-SELLER-02",
  "name": "Kho Quan 7 Trung Tam",
  "address": "So 456 Duong Nguyen Thi Thap, Tan Phu, Quan 7, TP. Ho Chi Minh",
  "contactPhone": "0909888999",
  "provinceName": "TP. Ho Chi Minh",
  "provinceId": 202,
  "districtId": 1450,
  "wardCode": "20804",
  "ghnShopId": 5550559,
  "isDisabled": false,
  "sellerId": "b7e51c20-72e1-4bb2-901a-82672bfdbef6"
}
```

## 6. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: Thieu `code` hoac `name`.
- **400 Bad Request (`WAREHOUSE_CODE_EXISTS`)**: Ma `code` da ton tai trong he thong.
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token hop le.
- **403 Forbidden (`FORBIDDEN`)**: Khong co quyen seller hoac admin.
