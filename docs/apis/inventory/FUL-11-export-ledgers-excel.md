# FUL-11: Xuat File Excel So Cai Kho (Export Ledgers Excel)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-11`
- **Ten API**: Export Ledgers Excel
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/ledgers/export`
  - Chay Local: `http://localhost:8006/api/v1/inventories/ledgers/export`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin`, `staff` (`[RequireRole("seller", "admin", "staff")]`)

## 2. Mo ta nghiep vu
Xuat toan bo hoac danh sach loc cac ban ghi So cai bien dong kho ra tap tin Microsoft Excel (`.xlsx`):
- MIME Type: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.
- Ten file: `so_cai_kho_{yyyyMMdd_HHmmss}.xlsx`.
- Chua day du thong tin: Ngay ghi so, Ma SKU, Kho hang, Loai bien dong, So luong nhap/xuat, So phieu tham chieu, va Ghi chu.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Authorization` | `Bearer <JWT_TOKEN>` | Co |

## 4. Query Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `warehouseId` | `Guid?` | Khong | Loc theo kho hang |
| `skuId` | `Guid?` | Khong | Loc theo SKU |
| `movementType` | `string?` | Khong | Loc theo loai bien dong |
| `fromDate` | `DateTimeOffset?` | Khong | Tu ngay |
| `toDate` | `DateTimeOffset?` | Khong | Den ngay |

## 5. Phan hoi thanh cong (200 OK)
- **Content-Type**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **Content-Disposition**: `attachment; filename=so_cai_kho_20261119_090000.xlsx`
- **Body**: File nhi phan (.xlsx).

## 6. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Khong co quyen truy cap.
