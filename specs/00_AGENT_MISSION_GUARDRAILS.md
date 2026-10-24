# 00 — NHIỆM VỤ CỐT LÕI & RÀO CHẮN CHỐNG ẢO GIÁC (FULFILLMENT SERVICE)
> **Trạng thái triển khai (2026-10-24):** ❌ Chưa có bề mặt — 5 ca sử dụng ở §1 là **đặc tả mục tiêu**; code hiện có **0/3 rpc**, **0 consumer**, **0 publisher**, **0 controller** và chỉ 1 endpoint `GET /health` (`Fulfillment.Api/Program.cs:57`). Ranh giới "Fulfillment sở hữu kho vật lý" là **đúng** và được giữ nguyên — xem §3.
> **Nguồn sự thật:** code trong repo này (`file:line` ghi kèm trong từng bảng).
> **Cập nhật lần cuối:** 2026-10-24.
---
## 1. NHIỆM VỤ & RANH GIỚI MIỀN (BOUNDED CONTEXT)
**FulfillmentService** chịu trách nhiệm độc quyền về việc quản lý kho vật lý và logistics giao vận:
- Quản lý tồn kho thực tế tại kho bãi vật lý (entity `InventoryItem`, khóa theo `SkuId` + `WarehouseId`): Số lượng tồn thực, vị trí ô kệ, trọng lượng và kích thước đóng gói. — ⚠️ Entity + 4 cột số lượng **đã có** (`Fulfillment.Domain/Entities/InventoryItem.cs:7-12`), nhưng **chưa có use case nào ghi** các cột này (0 lời gọi `SaveChangesAsync`). Vị trí ô kệ / trọng lượng / kích thước đóng gói **❌ không có trong entity** (đặc tả mục tiêu).
- Tính cước vận chuyển dự tính (`CalculateShippingFee`): Dựa trên địa chỉ người gửi (`SellerAddress`), địa chỉ người nhận (`BuyerAddress`) và trọng lượng kiện hàng tính bằng gam (`WeightGrams`). — ❌ **Đặc tả mục tiêu, chưa triển khai**: 0 rpc, 0 method trong `ShipmentService`/`WarehouseService`, entity `Shipment` không có trường trọng lượng/địa chỉ người gửi (`Fulfillment.Domain/Entities/Shipment.cs:8-19`).
- Quản lý và tra cứu trạng thái hành trình vận đơn (theo `TrackingNumber`, không phải `TrackingCode`): `ReadyToPick(0)` → `Picking(1)` → `Packed(2)` → `HandedOver(3)` → `InTransit(4)` → `Delivered(5)`, cùng `DeliveryFailed(6)`, `Returned(7)`, `Cancelled(8)`. — ⚠️ Enum trạng thái **có thật** (`Fulfillment.Domain/Enums/ShipmentStatus.cs`), tra cứu theo tracking **có thật** (`Fulfillment.Application/Services/Shipment/ShipmentService.cs:18-36`), nhưng **0 API/endpoint** phơi ra ngoài và **0 writer** cho `ShipmentEvent`.
  - Sửa tên sai so với tài liệu cũ: trạng thái `Preparing`/`PickedUp`/`OutForDelivery` **không tồn tại** trong enum `ShipmentStatus` (chúng thuộc enum khác — xem `ShipmentEventStatus`).
- Lắng nghe sự kiện thanh toán thành công để tự động tạo phiếu xuất kho và sinh mã vận đơn. — ❌ **Đặc tả mục tiêu, chưa triển khai**: 0 `IConsumer` trong repo. Tên event dùng chung là `IPaymentCompletedEvent` (không phải `OrderPaidEvent`).
- Cung cấp dịch vụ gRPC Server tại cổng `8006` cho ApiGateway. — ❌ **Chưa có server gRPC**: 0 `AddGrpc`, 0 `MapGrpcService`, 0 class kế thừa base gRPC. Port `8006` chỉ đúng trong Docker; chạy local là `5228`/`7014` (`Fulfillment.Api/Properties/launchSettings.json`).
---
## 2. NHỮNG ĐIỀU CẤM KỴ ĐỐI VỚI AI AGENTS (ANTI-HALLUCINATION GUARDRAILS)
1. **TUYỆT ĐỐI KHÔNG DÙNG REPOSITORY PATTERN BỌC NGOÀI EF CORE**:
   - Sử dụng trực tiếp `FulfillmentDbContext`/`AppDbContext` và `DbSet<T>` trong tầng Application.
   - Không được tạo `IShipmentRepository`, `IInventoryRepository`, `UnitOfWork`.
   - ✅ Hiện trạng: `Fulfillment.Application/Interfaces/IFulfillmentDbContext.cs` chỉ là abstraction của `DbContext` (10 `DbSet` + `SaveChangesAsync`), không phải repository.
2. **KHÔNG NHẦM LẪN VỚI KHO ẢO TRONG CATALOG SERVICE**:
   - Kho ảo trong `CatalogService` là số lượng `Available` hiển thị trên trang bán lẻ (trong code Catalog hiện **chỉ là đặc tả**, chưa có entity `Inventory`).
   - Kho trong `FulfillmentService` là kho vật lý (vị trí kho, trọng lượng, đóng gói thực tế), lưu ở cột `InventoryItem.AvailableQuantity` (`Fulfillment.Domain/Entities/InventoryItem.cs:12`).
   - **Ranh giới này là đúng và phải giữ**: Fulfillment sở hữu kho vật lý; Catalog chỉ sở hữu product/category.
3. **TUYỆT ĐỐI KHÔNG THAY ĐỔI CỔNG 8006 VÀ DATABASE 5438**:
   - Port đặc tả (Docker): `http://localhost:8006` (`Dockerfile:3-4`).
   - Port chạy local thật: `http://localhost:5228` / `https://localhost:7014` (`Fulfillment.Api/Properties/launchSettings.json`) — phải ghi cả hai, không được chỉ ghi `8006`.
   - Chuỗi kết nối DB trỏ tới `localhost:5438/fulfillment_db` với mật khẩu mặc định `MatkhaucuaTui123@` (fallback trong `Fulfillment.Infrastructure/DependencyInjection.cs:15`).
4. **KHÔNG ĐƯỢC MÔ TẢ NHƯ ĐÃ CÓ THỨ CHƯA TỒN TẠI**: Trước khi viết "service cung cấp rpc X" / "service publish event Y", phải grep được `MapGrpcService`/`IConsumer`/`IPublishEndpoint` tương ứng. Hiện tất cả đều **0**.
---
## 3. BIÊN GIỚI TỒN KHO: HAI CỘT SỐ LƯỢNG, CHƯA CÓ QUY TẮC ĐỒNG BỘ
Đây là điểm dễ ảo giác nhất khi viết tài liệu, nên chốt rõ:
| Khái niệm | Nơi sở hữu | Tên thật trong code | Bản chất | Trạng thái |
|---|---|---|---|---|
| Tồn kho **vật lý** | Fulfillment | `InventoryItem.AvailableQuantity` (+ `TotalQuantity`, `ReservedQuantity`, `UnavailableQuantity`) — `Fulfillment.Domain/Entities/InventoryItem.cs:9-12` | Số lượng thực tế có thể xuất khỏi kho | ⚠️ Có cột, có seed, **0 writer**; `AvailableQuantity` là **cột lưu song song** (denormalized), không được tính lại từ 3 cột còn lại |
| Tồn kho **ảo** (bán được) | Catalog | `Available` — mới chỉ nằm trong đặc tả `specs/` của Catalog, **chưa có entity trong code Catalog** | Số lượng hiển thị/cho bán trên sàn | ❌ Chưa triển khai |
**Sự thật bắt buộc phải ghi**: **không tồn tại quy tắc đồng bộ nào** giữa `InventoryItem.AvailableQuantity` (Fulfillment) và `Available` (Catalog) — không event, không job, không rpc, không transaction. Vì vậy **không được** viết "khi kho trừ thì Catalog tự cập nhật" hay ngược lại. Cả hai đang là hai con số độc lập, và phía Fulfillment thậm chí chưa có code ghi.
---
## 4. NHỮNG GÌ CODE THỰC SỰ LÀM ĐƯỢC HÔM NAY (để không ảo giác)
| Khả năng | Trạng thái | Bằng chứng |
|---|---|---|
| Trả `GET /health` | ✅ | `Fulfillment.Api/Program.cs:57` |
| Kết nối PostgreSQL + tự `Migrate()` khi khởi động (nuốt lỗi bằng warning) | ✅ | `Fulfillment.Api/Program.cs:40-55` |
| Đọc 1 `Shipment` theo `TrackingNumber` hoặc `OrderId` (in-process, chưa phơi ra ngoài) | ✅ | `Fulfillment.Application/Services/Shipment/ShipmentService.cs:18-56` |
| Đọc 1 `Warehouse` theo `Id`; liệt kê warehouse đang active | ✅ | `Fulfillment.Application/Services/Warehouse/WarehouseService.cs:20-53` |
| Tạo/giữ/giải phóng tồn kho | ❌ | 0 writer cho `InventoryItem`, `InventoryReservation`, `InventoryReservationItem`, `InventoryLedger` |
| Tạo vận đơn / cập nhật trạng thái vận đơn | ❌ | `CreateShipmentDto` khai báo nhưng 0 nơi dùng; 0 writer cho `Shipment`/`ShipmentEvent` |
| Tính cước vận chuyển | ❌ | 0 method, 0 rpc |
| Giao tiếp với service khác | ❌ | 0 consumer, 0 publisher, 0 gRPC client |