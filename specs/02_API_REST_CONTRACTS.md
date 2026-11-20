# 02 — HỢP ĐỒNG GIAO TIẾP RESTFUL (FULFILLMENT SERVICE REST CONTRACTS)

> **Trạng thái triển khai (2026-10-30):** ✅ ĐÃ CHUẨN HÓA 100% RESTFUL — Toàn bộ giao tiếp kho bãi và vận đơn qua RESTful Web API (`WarehousesController`, `ShipmentsController`). Bãi bỏ hoàn toàn gRPC và RabbitMQ.
> **Nguồn sự thật:** code (`Fulfillment.Api/Controllers/WarehousesController.cs`, `Fulfillment.Api/Controllers/ShipmentsController.cs`).
> **Cập nhật lần cuối:** 2026-10-30.

---

## 1. DANH MỤC ENDPOINTS RESTFUL CHÍNH THỨC

### 1.1. Vận đơn (`ShipmentsController` — `/api/v1/shipments`)
* `GET /api/v1/shipments/tracking/{trackingNumber}`: Tra cứu thông tin và lộ trình kiện hàng theo mã vận đơn.
* `GET /api/v1/shipments/order/{orderId}`: Tra cứu vận đơn gắn với một đơn hàng cụ thể.

### 1.2. Kho bãi (`WarehousesController` — `/api/v1/warehouses`)
* `GET /api/v1/warehouses/{id}`: Tra cứu thông tin chi tiết một kho bãi vật lý.

---

## 2. NGUYÊN TẮC BẤT BIẾN

* **Envelope & Errors**: Phản hồi bọc trong phong bì `ApiResponse<T>` hoặc `ApiResponse<PagedData<T>>`.
* **Casing**: DTO dùng PascalCase trong C# và camelCase trong JSON serialization.
* **1-to-1 Controller Pairing**: Mỗi controller chỉ inject duy nhất feature service tương ứng (`IShipmentService`, `IWarehouseService`).

---

## 3. KHO RIÊNG CỦA SELLER VÀ MÃ LỖI `SELLER_WAREHOUSE_*` (SP-05)

Một shop chỉ giao hàng từ kho của chính mình. Kho cần có **quận (`districtId`) và phường (`wardCode`)** thì hãng vận chuyển mới nhận hàng; một kho thiếu hai trường này là "chưa đầy đủ".

| Tình huống | Mã lỗi | HTTP | Nơi gặp |
|---|---|---|---|
| Seller không có kho đang hoạt động | `SELLER_WAREHOUSE_MISSING` | 422 | Báo phí vận chuyển của shop, tạo vận đơn |
| Có kho nhưng thiếu quận hoặc phường | `SELLER_WAREHOUSE_INCOMPLETE` | 422 | Báo phí vận chuyển của shop, tạo vận đơn |

* Không bao giờ dùng kho của seller khác hoặc kho nền tảng thay thế (sai địa chỉ lấy hàng, sai phí).
* `GET /api/v1/warehouses/me` (seller): trả kho đang hoạt động của chính seller. Mặc định, nếu chưa có kho thì tạo một kho giữ chỗ **không có quận, phường** (coi là "chưa đầy đủ"). Đặt cấu hình `Warehouses:ProvisionOnRead = false` để endpoint chỉ đọc: seller chưa có kho nhận `404 WAREHOUSE_NOT_FOUND`. **Chỉ đặt false sau khi mọi seller đã duyệt đều có kho** (khôi phục từ hồ sơ, kích hoạt lại kho cũ hoặc hoàn thiện kho giữ chỗ).
* `PUT /api/v1/warehouses/{id}` (chủ kho): hoàn thiện kho đã có; chỉ `name` bắt buộc, các trường khác chỉ cập nhật khi được gửi (`address`, `contactPhone`, `provinceName`, `districtId`, `wardCode`).
* Seller không tự tạo kho thứ hai: việc tạo hoặc khôi phục kho do luồng duyệt hồ sơ và lệnh thử lại của nhân viên thực hiện.

## 4. TRẠNG THÁI VẬN ĐƠN

* `Delivered` là trạng thái cuối của việc giao hàng: sự kiện `delivery_fail` đến muộn không đổi trạng thái và không tính là một lần giao thất bại. Sau `Delivered` chỉ còn `Returned` được chấp nhận.
