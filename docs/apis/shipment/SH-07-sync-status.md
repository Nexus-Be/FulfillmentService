# SH-07: Dong Bo Trang Thai Tu GHN Gateway (Sync Status)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SH-07`
- **Ten API**: Sync Status From Carrier Gateway
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/shipments/{id}/sync-status`
  - Chay Local: `http://localhost:8006/api/v1/shipments/{id}/sync-status`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin`, `staff` (`[RequireRole("seller", "admin", "staff")]`)

## 2. Mo ta nghiep vu
Chu dong goi sang API GHN de lay trang thai giao van moi nhat cua van don va cap nhat vao co so du lieu noi bo.

## 3. Path Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `id` | `Guid` | Co | Ma van don can dong bo |

## 4. Phan hoi thanh cong (200 OK)
Tra ve `ShipmentDto` voi trang thai va cac su kien giao van vua duoc dong bo.

## 5. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Khong so huu van don.
- **404 Not Found (`SHIPMENT_NOT_FOUND`)**: Khong tim thay van don.
- **502 Bad Gateway (`CARRIER_ERROR`)**: Loi ket noi voi GHN Gateway.
