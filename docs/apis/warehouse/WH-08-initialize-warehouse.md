# WH-08: Admin Khoi Tao Kho He Thong (Initialize Warehouse)

## 1. Thong tin tong quan
- **Ma chuc nang**: `WH-08`
- **Ten API**: Admin Initialize Warehouse
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses/initialize`
  - Chay Local: `http://localhost:8006/api/v1/warehouses/initialize`
- **Phan loai**: Public API
- **Quyen han / Role**: `admin` (`[RequireRole("admin")]`)

## 2. Mo ta nghiep vu
Admin he thong thiet lap kho mac dinh cho seller theo schema quan tri chuan hoa:
1. **Dau vao**: Nhan `InternalCreateWarehouseRequest` voi cac truong snake_case do UserService chuyen qua.
2. **Dau ra**: Tra ve HTTP 201 Created kem `InternalWarehouseResponse`.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Authorization` | `Bearer <JWT_ADMIN_TOKEN>` | Co |
| `Content-Type` | `application/json` | Co |

## 4. Request Body
```json
{
  "seller_id": "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f",
  "user_id": "d1e2f3a4-b5c6-4d7e-8f9a-0b1c2d3e4f5a",
  "shop_name": "Shop Flagship Official",
  "contact_name": "Nguyen Van Quan Ly",
  "contact_phone": "0912345678",
  "pickup_address": "123 Duong Le Duan, Quan 1, TP. Ho Chi Minh",
  "district_id": 1442,
  "ward_code": "20101",
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
- **400 Bad Request (`VAL_INVALID_INPUT`)**: Thieu `seller_id` hoac `shop_name`.
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Khong phai role admin.

## Xac minh dia chi khi luu (24/11/2026)
Doi bo `provinceId`, `districtId`, `wardCode` (ke ca chi doi mot phan, khi do dung gia tri da luu cho phan con lai) phai la ca chuoi va duoc doi chieu voi danh sach GHN truoc khi luu: bo ma sai `400 VAL_INVALID_LOCATION`, thieu mot phan `400 WAREHOUSE_ADDRESS_INCOMPLETE`, khong co nguon GHN dang tin cay `503 LOCATION_SOURCE_UNAVAILABLE` va khong luu gi. Chi doi ten hoac so dien thoai thi khong hoi danh sach GHN. Ten tinh gui len rong khong ghi de ten da xac nhan.
Kho giu cho da co san (tao khi seller mo trang kho) duoc hoan thien bang dia chi da xac nhan dau tien den; kho da day du duoc tra lai nguyen trang, khong hoi danh sach GHN. Body request co `province_id`; phan hoi chi gom `warehouse_id`, `seller_id`, `status`, `created_at`.
