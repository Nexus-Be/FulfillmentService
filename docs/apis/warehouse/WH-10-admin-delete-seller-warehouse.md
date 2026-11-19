# WH-10: Admin Xoa Kho Nguoi Ban (Admin Delete Seller Warehouse)

## 1. Thong tin tong quan
- **Ma chuc nang**: `WH-10`
- **Ten API**: Admin Delete Seller Warehouse
- **Phuong thuc**: `DELETE`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses/seller/{sellerId}`
  - Chay Local: `http://localhost:8006/api/v1/warehouses/seller/{sellerId}`
- **Phan loai**: Public API
- **Quyen han / Role**: `admin` (`[RequireRole("admin")]`)

## 2. Mo ta nghiep vu
Xoa kho hang cua mot nguoi ban khi nguoi ban dong gian hang hoac bi cam kinh doanh tren san.
- Dau ra: Tra ve HTTP `204 NoContent` theo chuan RESTful RFC 9110 khi xoa thanh cong hoac khong co kho de xoa.

## 3. Path Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `sellerId` | `Guid` (UUID) | Co | Ma nguoi ban can xoa kho |

## 4. Phan hoi thanh cong (204 NoContent)
*Khong co response body.*

## 5. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Khong phai role admin.
