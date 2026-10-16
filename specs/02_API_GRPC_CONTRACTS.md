# 02 — HỢP ĐỒNG GIAO TIẾP GRPC (FULFILLMENT SERVICE GRPC CONTRACTS)

Dịch vụ gRPC của FulfillmentService được định nghĩa trong hợp đồng duy nhất `Contracts/shop.proto` dưới service `FulfillmentApi`:

---

## 1. DANH SÁCH CÁC RPC METHODS

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

---

## 2. QUY CHUẨN XỬ LÝ LỖI & MÃ LỖI (ERROR STATUS CODES)

Khi xử lý trong `FulfillmentGrpcService`, ném ra `RpcException`:
- **`StatusCode.InvalidArgument`** (400 Bad Request): Địa chỉ gửi/nhận rỗng, trọng lượng `WeightGrams <= 0`, mã `SkuId` hoặc `TrackingCode` rỗng.
- **`StatusCode.NotFound`** (404 Not Found): Không tìm thấy SKU trong kho hoặc không tìm thấy mã vận đơn.
