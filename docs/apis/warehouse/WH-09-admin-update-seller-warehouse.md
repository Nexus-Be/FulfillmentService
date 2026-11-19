# WH-09: Admin Cap Nhat Kho Nguoi Ban (Admin Update Seller Warehouse)

## 1. Thong tin tong quan
- **Ma chuc nang**: `WH-09`
- **Ten API**: Admin Update Seller Warehouse
- **Phuong thuc**: `PUT`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses/seller/{sellerId}`
  - Chay Local: `http://localhost:8006/api/v1/warehouses/seller/{sellerId}`
- **Phan loai**: Public API
- **Quyen han / Role**: `admin` (`[RequireRole("admin")]`)

## 2. Mo ta nghiep vu
Admin he thong cap nhat thong tin dia chi lay hang, so dien thoai, va thong tin dia danh GHN cho kho cua mot nguoi ban:
1. **Dau vao**: Nhan `sellerId` tren Route va payload `InternalUpdateSellerWarehouseRequest` tren Body.
2. **Xu ly khong tim thay**: Neu seller chua co kho, tra ve HTTP 404 `WAREHOUSE_NOT_FOUND`.

## 3. Path Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `sellerId` | `Guid` (UUID) | Co | Ma nguoi ban can cap nhat kho |

## 4. Request Body
```json
{
  "shop_name": "Shop Flagship Official Moi",
  "contact_phone": "0988777666",
  "pickup_address": "456 Nguyen Dinh Chieu, Quan 3, TP. Ho Chi Minh",
  "district_id": 1444,
  "ward_code": "20302",
  "province_name": "TP. Ho Chi Minh"
}
```

## 5. Phan hoi thanh cong (200 OK)
```json
{
  "id": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
  "code": "WH_SELLER_1001",
  "name": "Shop Flagship Official Moi",
  "address": "456 Nguyen Dinh Chieu, Quan 3, TP. Ho Chi Minh",
  "contactPhone": "0988777666",
  "provinceName": "TP. Ho Chi Minh",
  "districtId": 1444,
  "wardCode": "20302",
  "ghnShopId": 5550559,
  "isDisabled": false,
  "sellerId": "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f"
}
```

## 6. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Khong phai role admin.
- **404 Not Found (`WAREHOUSE_NOT_FOUND`)**: Khong tim thay kho cua seller nay.
