# BO TAI LIEU API MODULE QUAN LY KHO BAI (WAREHOUSE MODULE APIS)

Module **Warehouse (Quan ly Kho bai)** chiu trach nhiem quan ly danh muc dia diem kho bai vat ly, so huu kho cua tung Nguoi ban (Seller) va trang thai hoat dong cua kho tren toan he thong san TMDT.

---

## Danh muc chi tiet tung API

| Ma | Ten API | Phuong thuc | Duong dan API | Quyen han | File tai lieu |
|---|---|---|---|---|---|
| `WH-01` | **List Active Warehouses** | `GET` | `/api/v1/warehouses` | Public | [WH-01-list-active-warehouses.md](WH-01-list-active-warehouses.md) |
| `WH-02` | **Get Warehouse By ID** | `GET` | `/api/v1/warehouses/{id}` | Public | [WH-02-get-warehouse-by-id.md](WH-02-get-warehouse-by-id.md) |
| `WH-03` | **Get Warehouse By Seller ID** | `GET` | `/api/v1/warehouses/seller/{sellerId}` | Public | [WH-03-get-warehouses-by-seller.md](WH-03-get-warehouses-by-seller.md) |
| `WH-04` | **Create Warehouse** | `POST` | `/api/v1/warehouses` | `seller`, `admin` | [WH-04-create-warehouse.md](WH-04-create-warehouse.md) |
| `WH-05` | **Update Warehouse** | `PUT` | `/api/v1/warehouses/{id}` | `seller`, `admin` | [WH-05-update-warehouse.md](WH-05-update-warehouse.md) |
| `WH-06` | **Set Active Status** | `PATCH` | `/api/v1/warehouses/{id}/status` | `seller`, `admin` | [WH-06-set-active-status.md](WH-06-set-active-status.md) |
| `WH-07` | **Get My Warehouse** | `GET` | `/api/v1/warehouses/me` | `seller` | [WH-07-get-my-warehouse.md](WH-07-get-my-warehouse.md) |
| `WH-08` | **Admin Initialize Warehouse** | `POST` | `/api/v1/warehouses/initialize` | `admin` | [WH-08-initialize-warehouse.md](WH-08-initialize-warehouse.md) |
| `WH-09` | **Admin Update Seller Warehouse** | `PUT` | `/api/v1/warehouses/seller/{sellerId}` | `admin` | [WH-09-admin-update-seller-warehouse.md](WH-09-admin-update-seller-warehouse.md) |
| `WH-10` | **Admin Delete Seller Warehouse** | `DELETE` | `/api/v1/warehouses/seller/{sellerId}` | `admin` | [WH-10-admin-delete-seller-warehouse.md](WH-10-admin-delete-seller-warehouse.md) |
| `WH-11` | **Internal Create Warehouse** | `POST` | `/api/v1/internal/warehouses` | Internal Service | [WH-11-internal-create-warehouse.md](WH-11-internal-create-warehouse.md) |

---

## Quy chuan kien truc & Bao mat
- **Port he thong**: Docker `8006` | Chay Local `8006`.
- **Phan quyen (RBAC)**: Kiem tra qua JWT Bearer token va danh tinh nguoi dung tu HttpContext.
- **Quy tac so huu**: Seller chi duoc sua hoac doi trang thai kho do chinh minh so huu.
- **Chuẩn bao loi**: RFC 9110 voi ma loi SCREAMING_SNAKE_CASE va thong diep tieng Anh Sentence case.
