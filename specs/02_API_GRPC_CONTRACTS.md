# 02 — HỢP ĐỒNG GIAO TIẾP GRPC (FULFILLMENT SERVICE GRPC CONTRACTS)
> **Trạng thái triển khai (2026-10-24):** ❌ Chưa có — **0/3 rpc** đã cài đặt (0 `AddGrpc`, 0 `MapGrpcService`, 0 class kế thừa base gRPC), và **file hợp đồng `Contracts/shop.proto` không tồn tại trong repo**. Bề mặt thật duy nhất là `GET /health` (`Fulfillment.Api/Program.cs:57`, HTTP/1.1 minimal endpoint).
> **Nguồn sự thật:** code trong repo này (`file:line` ghi kèm trong từng bảng).
> **Cập nhật lần cuối:** 2026-10-24.
Dịch vụ gRPC của FulfillmentService **được đặc tả** trong hợp đồng duy nhất `Contracts/shop.proto` dưới service `FulfillmentApi`:
- ❌ **Đường dẫn không tồn tại**: repo này **không có** thư mục `Contracts/` và không có file `shop.proto` (xác minh: `ls Fulfillment.Api/` → chỉ có `Middleware/`, `Program.cs`).
- ❌ **Proto package/reference không tồn tại**: `Fulfillment.Api.csproj` không có `PackageReference` `Grpc.AspNetCore`, `Grpc.Tools`, `Google.Protobuf`, cũng không có `<Protobuf Include=... />`.
---
## 1. DANH SÁCH CÁC RPC METHODS
Đây là **đặc tả mục tiêu (chưa triển khai)** — giữ nguyên để người đọc biết hợp đồng gốc:
```protobuf
service FulfillmentApi {
  // Tồn kho vật lý theo mã SKU
  rpc GetInventory(IdRequest) returns (FulfillmentInventory);
  // Tính cước vận chuyển dự tính
  rpc CalculateShippingFee(ShippingFeeRequest) returns (ShippingFeeQuote);
  // Tra cứu hành trình kiện hàng theo mã tracking
  rpc GetShipmentTracking(IdRequest) returns (ShipmentTracking);
}
```
| # | RPC | Input / Output (đặc tả) | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| 1 | `GetInventory` | `IdRequest` → `FulfillmentInventory` | ❌ Chưa có — đặc tả mục tiêu, chưa triển khai | grep `MapGrpcService` = 0; không có `shop.proto` |
| 2 | `CalculateShippingFee` | `ShippingFeeRequest` → `ShippingFeeQuote` | ❌ Chưa có — đặc tả mục tiêu, chưa triển khai | 0 method trong `ShipmentService`/`WarehouseService`; 0 rpc |
| 3 | `GetShipmentTracking` | `IdRequest` → `ShipmentTracking` | ❌ Chưa có — đặc tả mục tiêu, chưa triển khai | 0 rpc; 0 endpoint |
**Ghi chú thiết kế (lỗi tiềm ẩn của đặc tả gốc, chưa sửa vì là target)**:
- `GetShipmentTracking` nhận `IdRequest` trong khi mã vận đơn thật là `Shipment.TrackingNumber` kiểu `string?` (`Fulfillment.Domain/Entities/Shipment.cs:13`) — không phải `Guid`.
- `GetInventory` nhận `IdRequest` trong khi khóa tồn kho là cặp `(SkuId, WarehouseId)` (`Fulfillment.Infrastructure/Persistence/Configurations/InventoryItemConfiguration.cs:12`) — một `Id` đơn lẻ không đủ định danh.
- Hiện code **đã có** 4 truy vấn đọc tương ứng một phần (nhưng chưa phơi ra bề mặt): `ShipmentService.GetByTrackingNumberAsync` / `GetByOrderIdAsync` (`Fulfillment.Application/Services/Shipment/ShipmentService.cs:18-56`), `WarehouseService.GetByIdAsync` / `ListActiveAsync` (`Fulfillment.Application/Services/Warehouse/WarehouseService.cs:20-53`). Không có truy vấn nào theo `SkuId` và không có hàm tính cước.
### 1.1. Bề mặt thật đang tồn tại
| Bề mặt | Loại | Trạng thái | Bằng chứng |
|---|---|---|---|
| `GET /health` → `{"status":"Healthy","service":"Fulfillment.Api"}` | Minimal endpoint (HTTP/1.1) | ✅ | `Fulfillment.Api/Program.cs:57` |
| Swagger UI | Bật khi `IsDevelopment()` | ⚠️ có, nhưng chỉ thấy `/health` | `Fulfillment.Api/Program.cs:34-38` |
| Controller REST | — | ❌ 0 controller | `Fulfillment.Api/` không có `Controllers/` |
| gRPC | — | ❌ 0 service | grep `AddGrpc` = 0 |
---
## 2. QUY CHUẨN XỬ LÝ LỖI & MÃ LỖI (ERROR STATUS CODES)
**Đặc tả mục tiêu (chưa triển khai)**: Khi xử lý trong `FulfillmentGrpcService` (❌ class không tồn tại), ném ra `RpcException`:
- **`StatusCode.InvalidArgument`** (400 Bad Request): Địa chỉ gửi/nhận rỗng, trọng lượng `WeightGrams <= 0`, mã `SkuId` hoặc `TrackingCode` rỗng.
- **`StatusCode.NotFound`** (404 Not Found): Không tìm thấy SKU trong kho hoặc không tìm thấy mã vận đơn.
⚠️ Hiện **0 lời gọi `RpcException`** trong code (không có gRPC). Cơ chế lỗi thật đang dùng là HTTP ProblemDetails (RFC 9110) trong `GlobalExceptionHandlerMiddleware`:
| Exception | HTTP status | `errorCode` thật | Bằng chứng |
|---|---|---|---|
| `EntityNotFoundException` | 404 | `NOT_FOUND` | `Fulfillment.Api/Middleware/GlobalExceptionHandlerMiddleware.cs:38-42` |
| `BusinessRuleValidationException` | 400 | `BUSINESS_RULE_VIOLATION` | cùng file, dòng 43-47 |
| `DomainException` | 400 | `DOMAIN_ERROR` | cùng file, dòng 48-52 |
| `ArgumentException` | 400 | `VAL_INVALID_ARGUMENT` | cùng file, dòng 53-57 |
| Còn lại | 500 | `INTERNAL_SERVER_ERROR` | cùng file, dòng 58-63 |