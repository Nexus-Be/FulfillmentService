# BO TAI LIEU API MODULE QUAN LY VAN DON (SHIPMENT MODULE APIS)

Module **Shipment (Quan ly Van don)** chiu trach nhiem quan ly toan bo vong doi giao van kien hang, ket noi va dong bo truc tiep voi nha van chuyen Giao Hang Nhanh (GHN), phuc vu in nhan nhiet va xu ly hang hoan.

---

## Danh muc chi tiet tung API

| Ma | Ten API | Phuong thuc | Duong dan API | Quyen han | File tai lieu |
|---|---|---|---|---|---|
| `SH-01` | **List Shipments** | `GET` | `/api/v1/shipments` | `buyer`, `seller`, `admin`, `staff` | [SH-01-list-shipments.md](SH-01-list-shipments.md) |
| `SH-02` | **Get Shipment By ID** | `GET` | `/api/v1/shipments/{id}` | `buyer`, `seller`, `admin`, `staff` | [SH-02-get-shipment-by-id.md](SH-02-get-shipment-by-id.md) |
| `SH-03` | **Get By Tracking Number** | `GET` | `/api/v1/shipments/tracking/{trackingNumber}` | `buyer`, `seller`, `admin`, `staff` | [SH-03-get-by-tracking-number.md](SH-03-get-by-tracking-number.md) |
| `SH-04` | **Get By Order ID** | `GET` | `/api/v1/shipments/order/{orderId}` | `buyer`, `seller`, `admin`, `staff` | [SH-04-get-by-order-id.md](SH-04-get-by-order-id.md) |
| `SH-05` | **Cancel Shipment** | `POST` | `/api/v1/shipments/{id}/cancel` | `seller`, `admin` | [SH-05-cancel-shipment.md](SH-05-cancel-shipment.md) |
| `SH-06` | **Get Print Label** | `GET` | `/api/v1/shipments/{id}/print-label` | `seller`, `admin`, `staff` | [SH-06-get-print-label.md](SH-06-get-print-label.md) |
| `SH-07` | **Sync Status** | `POST` | `/api/v1/shipments/{id}/sync-status` | `seller`, `admin`, `staff` | [SH-07-sync-status.md](SH-07-sync-status.md) |
| `SH-08` | **Confirm Return** | `POST` | `/api/v1/shipments/orders/{orderId}/confirm-return` | `seller`, `admin` | [SH-08-confirm-return.md](SH-08-confirm-return.md) |
| `SH-09` | **Simulate Shipper Status** | `POST` | `/api/v1/shipments/{trackingNumber}/simulate-shipper-status` | `seller`, `admin`, `staff` | [SH-09-simulate-shipper-status.md](SH-09-simulate-shipper-status.md) |
| `SH-10` | **Internal Create Shipment** | `POST` | `/api/v1/internal/shipments` | Internal Service | [SH-10-internal-create-shipment.md](SH-10-internal-create-shipment.md) |
| `SH-11` | **Internal Cancel Shipment** | `POST` | `/api/v1/internal/shipments/order/{orderId}/cancel` | Internal Service | [SH-11-internal-cancel-shipment.md](SH-11-internal-cancel-shipment.md) |
| `SH-12` | **Internal Update Recipient** | `PUT` | `/api/v1/internal/shipments/order/{orderId}/recipient` | Internal Service | [SH-12-internal-update-recipient.md](SH-12-internal-update-recipient.md) |

---

## Bang anh xa trang thai van don (`ShipmentStatus`)
| Gia tri DB (int) | Gia tri API (PascalCase) | Y nghia nghiep vu |
|:---:|:---|:---|
| `0` | `ReadyToPick` | Da tao van don, cho shipper GHN den kho lay hang |
| `1` | `Picking` | Shipper dang den kho lay kien hang |
| `2` | `Packed` | Kien hang da dong goi luu tai kho |
| `3` | `HandedOver` | Da ban giao kien hang cho shipper GHN |
| `4` | `InTransit` | Kien hang dang luan chuyen giua cac trung tam khai thac |
| `5` | `Delivered` | Da giao hang thanh cong den tay nguoi nhan |
| `6` | `DeliveryFailed` | Giao hang that bai, cho giao lai |
| `7` | `Returned` | Hang hoan ve lai kho xuat hang |
| `8` | `Cancelled` | Van don da bi huy |
