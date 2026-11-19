# BO TAI LIEU API MODULE QUAN LY TON KHO (INVENTORY MODULE APIS)

Module **Inventory (Ton kho & Giu cho)** chiu trach nhiem quan ly so luong ton kho vat ly, nhat ky ke toan So cai (Stock Ledger) theo co che Append-only va toan bo vong doi giu cho don hang / phien dau gia (Reservation Lifecycle).

---

## Danh muc chi tiet tung API

| Ma | Ten API | Phuong thuc | Duong dan API | Quyen han | File tai lieu |
|---|---|---|---|---|---|
| `FUL-01` | **Initialize Inventory** | `POST` | `/api/v1/inventories/initialize` | `seller`, `admin`, `staff` | [FUL-01-declare-inventory.md](FUL-01-declare-inventory.md) |
| `FUL-02` | **Adjust Inventory** | `POST` | `/api/v1/inventories/adjust` | `seller`, `admin` | [FUL-02-adjust-inventory.md](FUL-02-adjust-inventory.md) |
| `FUL-03` | **Get Inventory By SKU** | `GET` | `/api/v1/inventories/{skuId}` | Public | [FUL-03-get-inventory-by-sku.md](FUL-03-get-inventory-by-sku.md) |
| `FUL-04` | **Get Inventory By List SKU** | `POST` | `/api/v1/inventories/by-skus` | Public | [FUL-04-get-inventory-by-list-sku.md](FUL-04-get-inventory-by-list-sku.md) |
| `FUL-05` | **Get Inventory Ledgers** | `GET` | `/api/v1/inventories/ledgers` | `seller`, `admin`, `staff` | [FUL-05-get-inventory-ledger.md](FUL-05-get-inventory-ledger.md) |
| `FUL-06` | **Create Reservation** | `POST` | `/api/v1/internal/inventories/reservations` | Internal Service | [FUL-06-create-reservation.md](FUL-06-create-reservation.md) |
| `FUL-07` | **Commit Reservation** | `POST` | `/api/v1/internal/inventories/reservations/{referenceId}/commit` | Internal Service | [FUL-07-commit-reservation.md](FUL-07-commit-reservation.md) |
| `FUL-08` | **Release Reservation** | `POST` | `/api/v1/internal/inventories/reservations/{referenceId}/release` | Internal Service | [FUL-08-release-reservation.md](FUL-08-release-reservation.md) |
| `FUL-09` | **Auto Expire Reservations** | `POST` | `/api/v1/internal/inventories/reservations/auto-expire` | System / Worker | [FUL-09-auto-expire-reservations.md](FUL-09-auto-expire-reservations.md) |
| `FUL-10` | **Batch Adjust Inventory** | `POST` | `/api/v1/inventories/batch-adjust` | `seller`, `admin` | [FUL-10-batch-adjust-inventory.md](FUL-10-batch-adjust-inventory.md) |
| `FUL-11` | **Export Ledgers Excel** | `GET` | `/api/v1/inventories/ledgers/export` | `seller`, `admin`, `staff` | [FUL-11-export-ledgers-excel.md](FUL-11-export-ledgers-excel.md) |
| `FUL-12` | **Get Ledgers By Reference** | `GET` | `/api/v1/inventories/ledgers/reference/{referenceType}/{referenceId}` | `seller`, `admin`, `staff` | [FUL-12-get-ledgers-by-reference.md](FUL-12-get-ledgers-by-reference.md) |
| `FUL-13` | **Import Preview AI** | `POST` | `/api/v1/inventories/import-preview` | `seller`, `admin` | [FUL-13-import-preview-ai.md](FUL-13-import-preview-ai.md) |
| `FUL-14` | **Batch Commit Import** | `POST` | `/api/v1/inventories/batch-commit-import` | `seller`, `admin` | [FUL-14-batch-commit-import.md](FUL-14-batch-commit-import.md) |
| `FUL-15` | **Update Stock Threshold** | `PUT` | `/api/v1/inventories/{skuId}/threshold` | `seller`, `admin` | [FUL-15-update-threshold.md](FUL-15-update-threshold.md) |
| `FUL-16` | **Low Stock Alerts** | `GET` | `/api/v1/inventories/alerts/low-stock` | `seller`, `admin`, `staff` | [FUL-16-low-stock-alerts.md](FUL-16-low-stock-alerts.md) |
| `FUL-17` | **Dead Stock Alerts** | `GET` | `/api/v1/inventories/alerts/dead-stock` | `seller`, `admin`, `staff` | [FUL-17-dead-stock-alerts.md](FUL-17-dead-stock-alerts.md) |
| `FUL-18` | **Inventory Analytics** | `GET` | `/api/v1/inventories/analytics` | `seller`, `admin`, `staff` | [FUL-18-inventory-analytics.md](FUL-18-inventory-analytics.md) |
| `FUL-19` | **Partial Release Reservation** | `POST` | `/api/v1/internal/inventories/reservations/{referenceId}/release-items` | Internal Service | [FUL-19-partial-release-reservation.md](FUL-19-partial-release-reservation.md) |
| `FUL-20` | **Batch Release Reservations** | `POST` | `/api/v1/internal/inventories/reservations/batch-release` | Internal Service | [FUL-20-batch-release-reservations.md](FUL-20-batch-release-reservations.md) |
| `FUL-21` | **Auction Reserve** | `POST` | `/api/v1/internal/fulfillment/auction-reserve` | Internal Service | [FUL-21-auction-reserve.md](FUL-21-auction-reserve.md) |
| `FUL-22` | **Auction Release** | `POST` | `/api/v1/internal/fulfillment/auction-release/{referenceId}` | Internal Service | [FUL-22-auction-release.md](FUL-22-auction-release.md) |

---

## Quy chuan kien truc & Bao mat
- **Port he thong**: Docker `8006` | Chay Local `8006`.
- **So cai bat bien**: `InventoryLedgers` chi INSERT va SELECT, tuyet doi cam UPDATE hoac DELETE.
- **Idempotency**: Chot don va huy giu cho bao ve chong retry bang cach doi soat dong so cai da commit.
