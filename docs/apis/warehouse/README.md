# BỘ TÀI LIỆU API MODULE QUẢN LÝ KHO BÃI (WAREHOUSE MODULE APIS)

Module **Warehouse (Quản lý Kho bãi)** chịu trách nhiệm quản lý danh mục địa điểm kho bãi vật lý, sở hữu kho của từng Người bán (Seller) và trạng thái hoạt động của kho trên toàn hệ thống sàn TMĐT.

---

## 📌 Danh mục chi tiết từng API:

| Mã | Tên API | Phương thức | Đường dẫn API | Quyền hạn | File tài liệu |
|---|---|---|---|---|---|
| `WH-01` | **List Active Warehouses** | `GET` | `/api/v1/warehouses` | Public | [WH-01-list-active-warehouses.md](WH-01-list-active-warehouses.md) |
| `WH-02` | **Get Warehouse By ID** | `GET` | `/api/v1/warehouses/{id}` | Public | [WH-02-get-warehouse-by-id.md](WH-02-get-warehouse-by-id.md) |
| `WH-03` | **Get Warehouses By Seller ID** | `GET` | `/api/v1/warehouses/seller/{sellerId}` | Public | [WH-03-get-warehouses-by-seller.md](WH-03-get-warehouses-by-seller.md) |
| `WH-04` | **Create Warehouse** | `POST` | `/api/v1/warehouses` | `seller`, `admin` | [WH-04-create-warehouse.md](WH-04-create-warehouse.md) |
| `WH-05` | **Update Warehouse** | `PUT` | `/api/v1/warehouses/{id}` | `seller`, `admin` | [WH-05-update-warehouse.md](WH-05-update-warehouse.md) |
| `WH-06` | **Set Active Status** | `PATCH` | `/api/v1/warehouses/{id}/status` | `admin` | [WH-06-set-active-status.md](WH-06-set-active-status.md) |

---

## 🔒 Quy chuẩn kiến trúc & Bảo mật:
- **Port hệ thống**: Docker `8006` | Chạy Local `5228` (HTTP) / `7014` (HTTPS).
- **Phân quyền (RBAC)**: Dựa trên Header chuẩn hóa `X-User-Roles` được API Gateway chuyển giao sau khi giải mã JWT.
- **Ràng buộc toàn vẹn**:
  - Mã kho (`Code`) tự động chuẩn hóa viết hoa và là duy nhất trong toàn hệ thống (`Unique Index`).
  - Không cho phép cập nhật `Code` sau khi tạo.
- **Chuẩn báo lỗi**: Tuân thủ RFC 7807 ProblemDetails với mã lỗi SCREAMING_SNAKE_CASE (ví dụ: `WAREHOUSE_NOT_FOUND`, `FORBIDDEN`, `VAL_INVALID_INPUT`).

