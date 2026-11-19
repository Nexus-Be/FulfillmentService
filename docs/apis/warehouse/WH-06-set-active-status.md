# WH-06: Bat/Tat Trang Thai Hoat Dong Cua Kho (Set Warehouse Active Status)

## 1. Thong tin tong quan
- **Ma chuc nang**: `WH-06`
- **Ten API**: Set Warehouse Active Status (Activate / Deactivate)
- **Phuong thuc**: `PATCH`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses/{id}/status?isActive={true|false}`
  - Chay Local: `http://localhost:8006/api/v1/warehouses/{id}/status?isActive={true|false}`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin` (`[RequireRole("seller", "admin")]`)

## 2. Mo ta nghiep vu
Bat hoac tat trang thai hoat dong cua kho bai (dong mo kho):
1. **Phan quyen & Kiem tra so huu**:
   - Seller chi duoc phep thay doi trang thai kho do chinh minh so huu (`warehouse.SellerId == userId`).
   - Admin co quyen can thiep doi trang thai bat ky kho nao.
2. **Hieu ung khi vo hieu hoa (`isActive = false`)**:
   - Kho bi an khoi danh sach tra cuu kho hoat dong (`WH-01`).
   - Khong cho phep tao van don xuat kho moi tu kho da bi tat.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Authorization` | `Bearer <JWT_TOKEN>` | Co |

## 4. Path & Query Parameters
| Tham so | Vi tri | Kieu | Bat buoc | Mo ta |
|---|---|---|---|---|
| `id` | Path | `Guid` (UUID) | Co | Ma dinh danh kho bai |
| `isActive` | Query | `bool` | Co | `true`: Mo kho hoat dong, `false`: Dong tam khoa kho |

## 5. Phan hoi thanh cong (200 OK)
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
  "isDisabled": true,
  "sellerId": "b7e51c20-72e1-4bb2-901a-82672bfdbef6"
}
```

## 6. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Nguoi ban khong so huu kho hang nay.
- **404 Not Found (`WAREHOUSE_NOT_FOUND`)**: Khong tim thay kho voi ID cung cap.
