# 00 — NHIỆM VỤ CỐT LÕI & RÀO CHẮN CHỐNG ẢO GIÁC (FULFILLMENT SERVICE)

---

## 1. NHIỆM VỤ & RANH GIỚI MIỀN (BOUNDED CONTEXT)

**FulfillmentService** chịu trách nhiệm độc quyền về việc quản lý kho vật lý và logistics giao vận:
- Quản lý tồn kho thực tế tại kho bãi vật lý (`FulfillmentInventory` theo `SkuId`): Số lượng tồn thực, vị trí ô kệ, trọng lượng và kích thước đóng gói.
- Tính cước vận chuyển dự tính (`CalculateShippingFee`): Dựa trên địa chỉ người gửi (`SellerAddress`), địa chỉ người nhận (`BuyerAddress`) và trọng lượng kiện hàng tính bằng gam (`WeightGrams`).
- Quản lý và tra cứu trạng thái hành trình vận đơn (`ShipmentTracking` theo `TrackingCode`): `Preparing` $\rightarrow$ `PickedUp` $\rightarrow$ `InTransit` $\rightarrow$ `OutForDelivery` $\rightarrow$ `Delivered`.
- Lắng nghe sự kiện thanh toán thành công `OrderPaidEvent` từ `CommerceService` để tự động tạo phiếu xuất kho và sinh mã vận đơn.
- Cung cấp dịch vụ gRPC Server tại cổng `8006` cho ApiGateway.

---

## 2. NHỮNG ĐIỀU CẤM KỴ ĐỐI VỚI AI AGENTS (ANTI-HALLUCINATION GUARDRAILS)

1. **TUYỆT ĐỐI KHÔNG DÙNG REPOSITORY PATTERN BỌC NGOÀI EF CORE**:
   - Sử dụng trực tiếp `FulfillmentDbContext` và `DbSet<T>` trong tầng Application.
   - Không được tạo `IShipmentRepository`, `IInventoryRepository`, `UnitOfWork`.

2. **KHÔNG NHẦM LẪN VỚI KHO ẢO TRONG CATALOG SERVICE**:
   - Kho ảo trong `CatalogService` là số lượng `Available` hiển thị trên trang bán lẻ.
   - Kho trong `FulfillmentService` là kho vật lý (vị trí kho, trọng lượng, đóng gói thực tế).

3. **TUYỆT ĐỐI KHÔNG THAY ĐỔI CỔNG 8006 VÀ DATABASE 5437**:
   - Service gRPC chạy tại `http://localhost:8006`.
   - Chuỗi kết nối DB trỏ tới `localhost:5437/fulfillment_db`.
