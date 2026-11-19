# TONG HOP HE THONG TAI LIEU API FULFILLMENT SERVICE (PORT 8006)

Tai lieu dac ta toan dien 100% cac giao dien lap trinh ung dung (RESTful Web API) cua phan he **Fulfillment Service (Quan ly Kho bai, Ton kho, Van don & Giao van)** trong he sinh thai Nexus.

---

## 1. Ban do phan he tinh nang (Feature Modules)

### 1.1. Module Kho bai (`warehouse/` — 11 APIs)
Chi tiet tai [warehouse/README.md](warehouse/README.md):
- `WH-01`: `GET /api/v1/warehouses` — Lay danh sach kho dang hoat dong (Phan trang PagedData).
- `WH-02`: `GET /api/v1/warehouses/{id}` — Lay chi tiet kho theo ID.
- `WH-03`: `GET /api/v1/warehouses/seller/{sellerId}` — Lay kho cua seller theo sellerId.
- `WH-04`: `POST /api/v1/warehouses` — Dang ky kho hang moi.
- `WH-05`: `PUT /api/v1/warehouses/{id}` — Cap nhat thong tin kho hang.
- `WH-06`: `PATCH /api/v1/warehouses/{id}/status` — Bat/tat trang thai hoat dong cua kho.
- `WH-07`: `GET /api/v1/warehouses/me` — Tra cuu hoac tu dong cap phat kho mac dinh cho Seller.
- `WH-08`: `POST /api/v1/warehouses/initialize` — Admin khoi tao kho he thong.
- `WH-09`: `PUT /api/v1/warehouses/seller/{sellerId}` — Admin cap nhat kho cua Seller.
- `WH-10`: `DELETE /api/v1/warehouses/seller/{sellerId}` — Admin xoa kho cua Seller (204 NoContent).
- `WH-11`: `POST /api/v1/internal/warehouses` — Tao kho noi bo khi duyet ho so dang ky nguoi ban.

### 1.2. Module Ton kho & Giu cho (`inventory/` — 22 APIs)
Chi tiet tai [inventory/README.md](inventory/README.md):
- `FUL-01`: `POST /api/v1/inventories/initialize` — Khoi tao ton kho ban dau cho SKU.
- `FUL-02`: `POST /api/v1/inventories/adjust` — Nhap kho hoac dieu chinh kiem ke (Intake / Adjust).
- `FUL-03`: `GET /api/v1/inventories/{skuId}` — Lay thong tin ton kho theo 1 SKU.
- `FUL-04`: `POST /api/v1/inventories/by-skus` — Lay ton kho danh sach SKU hang loat.
- `FUL-05`: `GET /api/v1/inventories/ledgers` — Lay lich su bien dong So cai kho kem thong ke Summary.
- `FUL-06`: `POST /api/v1/internal/inventories/reservations` — Giu cho ton kho tam thoi (Hold 30 phut).
- `FUL-07`: `POST /api/v1/internal/inventories/reservations/{referenceId}/commit` — Chot ban hang & xuat kho that su.
- `FUL-08`: `POST /api/v1/internal/inventories/reservations/{referenceId}/release` — Huy giu cho ton kho.
- `FUL-09`: `POST /api/v1/internal/inventories/reservations/auto-expire` — Quet & huy phieu giu cho qua han.
- `FUL-10`: `POST /api/v1/inventories/batch-adjust` — Nhap kho & dieu chinh ton kho hang loat nguyen tu.
- `FUL-11`: `GET /api/v1/inventories/ledgers/export` — Xuat file Excel (.xlsx) so cai bien dong kho.
- `FUL-12`: `GET /api/v1/inventories/ledgers/reference/{referenceType}/{referenceId}` — Tra cuu so cai theo ma chung tu tham chieu.
- `FUL-13`: `POST /api/v1/inventories/import-preview` — AI trich xuat & xem truoc du lieu nhap kho tu Excel/CSV.
- `FUL-14`: `POST /api/v1/inventories/batch-commit-import` — Xac nhan ghi nhap kho tu ket qua import.
- `FUL-15`: `PUT /api/v1/inventories/{skuId}/threshold` — Cap nhat nguong canh bao ton kho thap.
- `FUL-16`: `GET /api/v1/inventories/alerts/low-stock` — Danh sach san pham duoi nguong ton kho an toan.
- `FUL-17`: `GET /api/v1/inventories/alerts/dead-stock` — Danh sach hang ton dong lau ngay khong phat sinh giao dich.
- `FUL-18`: `GET /api/v1/inventories/analytics` — Bao cao tong quan phan tich ton kho va toc do ban hang.
- `FUL-19`: `POST /api/v1/internal/inventories/reservations/{referenceId}/release-items` — Giai phong mot phan hang giu cho.
- `FUL-20`: `POST /api/v1/internal/inventories/reservations/batch-release` — Giai phong hang loat phieu giu cho.
- `FUL-21`: `POST /api/v1/internal/fulfillment/auction-reserve` — Khoa giu cho hang phuc vu phien dau gia truc tiep.
- `FUL-22`: `POST /api/v1/internal/fulfillment/auction-release/{referenceId}` — Giai phong hang dau gia bi huy hoac het han.

### 1.3. Module Van don (`shipment/` — 12 APIs)
Chi tiet tai [shipment/README.md](shipment/README.md):
- `SH-01`: `GET /api/v1/shipments` — Danh sach van don phan trang kem bo loc da tieu chi.
- `SH-02`: `GET /api/v1/shipments/{id}` — Lay chi tiet van don theo ID kem danh sach kien hang va su kien.
- `SH-03`: `GET /api/v1/shipments/tracking/{trackingNumber}` — Tra cuu van don theo ma van don GHN.
- `SH-04`: `GET /api/v1/shipments/order/{orderId}` — Tra cuu van don theo don hang con.
- `SH-05`: `POST /api/v1/shipments/{id}/cancel` — Huy van don va dong bo sang hang GHN.
- `SH-06`: `GET /api/v1/shipments/{id}/print-label` — Lay token va URL in nhan van don nhiet A5/80x80.
- `SH-07`: `POST /api/v1/shipments/{id}/sync-status` — Dong bo trang thai van don truc tiep tu GHN Gateway.
- `SH-08`: `POST /api/v1/shipments/orders/{orderId}/confirm-return` — Seller xac nhan hang hoan va tu dong phuc hoi ton kho.
- `SH-09`: `POST /api/v1/shipments/{trackingNumber}/simulate-shipper-status` — Gia lap buu ta cap nhat chang giao van sandbox.
- `SH-10`: `POST /api/v1/internal/shipments` — Tao van don noi bo do Commerce Service yeu cau.
- `SH-11`: `POST /api/v1/internal/shipments/order/{orderId}/cancel` — Commerce yeu cau huy van don theo don hang con.
- `SH-12`: `PUT /api/v1/internal/shipments/order/{orderId}/recipient` — Cap nhat dia chi nguoi nhan truoc khi buu ta lay hang.

### 1.4. Module Cuoc phi & Dia ban (`shipping/` — 6 APIs)
Chi tiet tai [shipping/README.md](shipping/README.md):
- `SP-01`: `GET /api/v1/shipping/locations/provinces` — Danh muc 63 Tinh/Thanh pho GHN (Cache 24h).
- `SP-02`: `GET /api/v1/shipping/locations/districts` — Danh muc Quan/Huyen theo Tinh (Cache 24h).
- `SP-03`: `GET /api/v1/shipping/locations/wards` — Danh muc Phuong/Xa theo Huyen (Cache 24h).
- `SP-04`: `GET /api/v1/shipping/services` — Danh muc goi cuoc van chuyen kha dung.
- `SP-05`: `POST /api/v1/shipping/quote` — Tinh toan phi van chuyen tam tinh cho gio hang va PDP.
- `SP-06`: `POST /api/v1/internal/shipping/locations/validate` — Tham dinh tinh hop le va nhat quan cua dia chi 3 cap.

### 1.5. Module Webhook GHN (`webhook/` — 1 API)
Chi tiet tai [webhook/README.md](webhook/README.md):
- `WHK-01`: `POST /api/v1/webhooks/ghn` — Tiep nhan webhook doi trang thai tu GHN, bao mat token, chong lap idempotency va Outbox sang Commerce.

---

## 2. Quy chuan kien truc & Ranh gioi
1. **100% Inward-Only**: Toan bo tai lieu chi tham chieu noi bo toi cac thanh phan cua FulfillmentService.
2. **Pure DTO Contract**: Toan bo DTO tra ve la Pure DTO hoac `PagedData<T>`, khong boc qua `ApiResponse<T>`.
3. **Chuan hoa Ma loi**: RFC 9110 voi `errorCode` dang SCREAMING_SNAKE_CASE va `message` tieng Anh Sentence case.
