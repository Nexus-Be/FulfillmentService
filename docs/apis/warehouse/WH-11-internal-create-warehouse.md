# WH-11: Tao Kho Hang Noi Bo Cho Nguoi Ban (Internal Create Warehouse)

## 1. Thong tin tong quan
- **Ma chuc nang**: `WH-11`
- **Ten API**: Internal Create Warehouse (Seller Onboarding)
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/warehouses`
  - Chay Local: `http://localhost:8006/api/v1/internal/warehouses`
- **Phan loai**: Internal API (Goi tu UserService khi Admin duyet ho so dang ky nguoi ban `SellerRegistration`)
- **Quyen han / Role**: Internal Service

## 2. Mo ta nghiep vu
Tu dong thiet lap kho xuat hang vat ly cho nguoi ban ngay khi tai khoan duoc phe duyet ban hang tren he thong.
- Nhan thong tin dia chi lay hang, ten gian hang, va tu dong phat sinh ma kho `WH_SELLER_{ID}`.
- Tra ve HTTP 201 Created kem `warehouse_id`.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Content-Type` | `application/json` | Co |

## 4. Request Body
```json
{
  "seller_id": "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f",
  "user_id": "d1e2f3a4-b5c6-4d7e-8f9a-0b1c2d3e4f5a",
  "shop_name": "Shop Thoi Trang Bella",
  "contact_name": "Tran Thi B",
  "contact_phone": "0911222333",
  "pickup_address": "789 Hai Ba Trung, Quan 3, TP. Ho Chi Minh",
  "province_id": 202,
  "district_id": 1444,
  "ward_code": "20302",
  "province_name": "TP. Ho Chi Minh"
}
```

## 5. Phan hoi thanh cong (201 Created)
```json
{
  "warehouse_id": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
  "seller_id": "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f",
  "status": "Active",
  "created_at": "2026-11-19T09:00:00Z"
}
```

## 6. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: Thieu `seller_id` hoac `pickup_address`.

## Xac minh dia chi khi luu (24/11/2026)
Doi bo `provinceId`, `districtId`, `wardCode` (ke ca chi doi mot phan, khi do dung gia tri da luu cho phan con lai) phai la ca chuoi va duoc doi chieu voi danh sach GHN truoc khi luu: bo ma sai `400 VAL_INVALID_LOCATION`, thieu mot phan `400 WAREHOUSE_ADDRESS_INCOMPLETE`, khong co nguon GHN dang tin cay `503 LOCATION_SOURCE_UNAVAILABLE` va khong luu gi. Chi doi ten hoac so dien thoai thi khong hoi danh sach GHN. Ten tinh gui len rong khong ghi de ten da xac nhan.
Kho giu cho da co san (tao khi seller mo trang kho) duoc hoan thien bang dia chi da xac nhan dau tien den; kho da day du duoc tra lai nguyen trang, khong hoi danh sach GHN. Body request co `province_id`; phan hoi chi gom `warehouse_id`, `seller_id`, `status`, `created_at`.
