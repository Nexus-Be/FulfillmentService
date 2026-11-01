# BỘ TÀI LIỆU API MODULE QUẢN LÝ KHO (INVENTORY MODULE APIS)

Module **Inventory (Kho bãi & Giữ chỗ)** chịu trách nhiệm quản lý tồn kho vật lý thực tế, nhật ký kế toán Sổ cái (Stock Ledger) và toàn bộ vòng đời giữ chỗ đơn hàng (Reservation Lifecycle).

---

## 📌 Danh mục tài liệu chi tiết từng API:

| Mã | Tên API | Phương thức | Đường dẫn Internal | File tài liệu |
|---|---|---|---|---|
| `FUL-01` | **Declare Inventory** | `POST` | `/api/v1/internal/inventories/declare` | [FUL-01-declare-inventory.md](FUL-01-declare-inventory.md) |
| `FUL-02` | **Adjust Inventory** | `POST` | `/api/v1/internal/inventories/adjust` | [FUL-02-adjust-inventory.md](FUL-02-adjust-inventory.md) |
| `FUL-03` | **Get Inventory By SKU** | `GET` | `/api/v1/internal/inventories/{skuId}` | [FUL-03-get-inventory-by-sku.md](FUL-03-get-inventory-by-sku.md) |
| `FUL-04` | **Get Inventory By List SKU** | `POST` | `/api/v1/internal/inventories/batch-get` | [FUL-04-get-inventory-by-list-sku.md](FUL-04-get-inventory-by-list-sku.md) |
| `FUL-05` | **Get Inventory Ledger** | `GET` | `/api/v1/internal/inventories/{skuId}/ledgers` | [FUL-05-get-inventory-ledger.md](FUL-05-get-inventory-ledger.md) |
| `FUL-06` | **Create Reservation** | `POST` | `/api/v1/internal/inventories/reservations` | [FUL-06-create-reservation.md](FUL-06-create-reservation.md) |
| `FUL-07` | **Commit Reservation** | `POST` | `/api/v1/internal/inventories/reservations/{referenceId}/commit` | [FUL-07-commit-reservation.md](FUL-07-commit-reservation.md) |
| `FUL-08` | **Release Reservation** | `POST` | `/api/v1/internal/inventories/reservations/{referenceId}/release` | [FUL-08-release-reservation.md](FUL-08-release-reservation.md) |
| `FUL-09` | **Auto Expire Reservations** | `POST` | `/api/v1/internal/inventories/reservations/auto-expire` | [FUL-09-auto-expire-reservations.md](FUL-09-auto-expire-reservations.md) |

---

## 🔒 Quy chuẩn hệ thống:
- **Port**: Docker `8006` | Local `5228` (HTTP) / `7014` (HTTPS).
- **Error Standard**: RFC 7807 ProblemDetails với mã lỗi SCREAMING_SNAKE_CASE và thông điệp tiếng Anh.
- **Idempotency**: Bảo vệ chống gọi lặp (duplicate calls) ở tất cả các luồng thanh toán và xuất/hủy kho.

