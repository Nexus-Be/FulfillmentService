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
