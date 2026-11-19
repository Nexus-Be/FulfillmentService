# WH-05: Cap Nhat Thong Tin Kho (Update Warehouse)

## 1. Thong tin tong quan
- **Ma chuc nang**: `WH-05`
- **Ten API**: Update Warehouse
- **Phuong thuc**: `PUT`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses/{id}`
  - Chay Local: `http://localhost:8006/api/v1/warehouses/{id}`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin` (`[RequireRole("seller", "admin")]`)

## 2. Mo ta nghiep vu
Cap nhat thong tin kho bai vat ly:
1. **Bat bien ma kho (`Code`)**: `Code` khong the thay doi sau khi tao nham bao toan tinh toan ven du lieu van don va chung tu kho.
2. **Kiem tra quyen so huu (Ownership Guard)**: Seller chi duoc phep cap nhat kho do chinh minh so huu (`warehouse.SellerId == userId`). Neu vi pham, he thong tra ve HTTP 403 `FORBIDDEN`.
3. **Cap nhat thong tin dia danh & GHN Shop ID**: Ho tro cap nhat so dien thoai kho, quan huyen, phuong xa va GHN Shop ID de dam bao dong bo lay hang tu nha van chuyen.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Authorization` | `Bearer <JWT_TOKEN>` | Co |
| `Content-Type` | `application/json` | Co |

## 4. Path Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `id` | `Guid` (UUID) | Co | Ma dinh danh kho can cap nhat |

## 5. Request Body
```json
{
  "name": "Kho Quan 7 Mo Rong",
  "address": "So 458 Duong Nguyen Thi Thap, Tan Phu, Quan 7, TP. Ho Chi Minh",
  "contactPhone": "0909888777",
  "provinceName": "TP. Ho Chi Minh",
  "districtId": 1450,
  "wardCode": "20804",
  "ghnShopId": 5550559
}
```

## 6. Phan hoi thanh cong (200 OK)
```json
{
  "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "code": "WH-SELLER-02",
  "name": "Kho Quan 7 Mo Rong",
  "address": "So 458 Duong Nguyen Thi Thap, Tan Phu, Quan 7, TP. Ho Chi Minh",
  "contactPhone": "0909888777",
  "provinceName": "TP. Ho Chi Minh",
  "districtId": 1450,
  "wardCode": "20804",
  "ghnShopId": 5550559,
  "isDisabled": false,
  "sellerId": "b7e51c20-72e1-4bb2-901a-82672bfdbef6"
}
```

## 7. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: Ten kho bi de trong hoac du lieu khong hop le.
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Nguoi ban khong so huu kho hang nay.
- **404 Not Found (`WAREHOUSE_NOT_FOUND`)**: Khong tim thay kho voi ID cung cap.
