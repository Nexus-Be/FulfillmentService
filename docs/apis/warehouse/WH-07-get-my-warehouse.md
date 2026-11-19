# WH-07: Tra Cuu Hoac Tu Dong Cap Phat Kho Cho Seller (Get My Warehouse)

## 1. Thong tin tong quan
- **Ma chuc nang**: `WH-07`
- **Ten API**: Get My Warehouse (Self-Provisioning)
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses/me`
  - Chay Local: `http://localhost:8006/api/v1/warehouses/me`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller` (`[RequireRole("seller")]`)

## 2. Mo ta nghiep vu
Lay thong tin kho hang cua Seller dang dang nhap dua vao JWT claim `sub` (`userId`):
1. **Tu dong khoi tao (Provisioning on-demand)**: Neu Seller chua co kho nao trong he thong, ham `GetOrProvisionForSellerAsync` se tu dong khoi tao mot kho mac dinh mang ten shop va dia chi tu query parameter, giup nguoi ban co the bat dau quan ly ton kho va tao van don ngay lap tuc.
2. **Xu ly that bai**: Neu khong the tu cap phat, tra ve HTTP 404 `WAREHOUSE_NOT_FOUND`.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Authorization` | `Bearer <JWT_SELLER_TOKEN>` | Co |

## 4. Query Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `sellerId` | `Guid?` | Khong | Seller profile ID |
| `shopName` | `string?` | Khong | Ten shop de dat ten kho khi tu provision |
| `address` | `string?` | Khong | Dia chi kho khi tu provision |

## 5. Phan hoi thanh cong (200 OK)
```json
{
  "id": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
  "code": "WH_SELLER_1001",
  "name": "Kho Shop Thoi Trang",
  "address": "123 Truong Chinh, Quan Tan Binh, TP. Ho Chi Minh",
  "contactPhone": "0901234567",
  "provinceName": "TP. Ho Chi Minh",
  "districtId": 1442,
  "wardCode": "20101",
  "ghnShopId": 5550559,
  "isDisabled": false,
  "sellerId": "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f"
}
```

## 6. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Khong phai role seller.
- **404 Not Found (`WAREHOUSE_NOT_FOUND`)**: Khong tim thay kho.
