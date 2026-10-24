# 03 — SỰ KIỆN BẤT ĐỒNG BỘ QUA RABBITMQ (FULFILLMENT SERVICE EVENT MESSAGING)
> **Trạng thái triển khai (2026-10-24):** ❌ Chưa có luồng message — **0/1 consumer** đã cài đặt, **0/3 event** được publish (grep `IConsumer` = 0, `IPublishEndpoint`/`IBus`/`.Publish(` = 0). Hạ tầng thì **đã dựng**: MassTransit **8.3.0** + RabbitMQ + EF Transactional Outbox (3 bảng `InboxState`/`OutboxMessage`/`OutboxState` có trong migration), nhưng không có message nào đi qua.
> **Nguồn sự thật:** code trong repo này (`file:line` ghi kèm trong từng bảng).
> **Cập nhật lần cuối:** 2026-10-24.
FulfillmentService tham gia vào giai đoạn sau thanh toán (post-checkout fulfillment) khi đơn hàng đã được trả tiền thành công.
---
## 1. SỰ KIỆN LẮNG NGHE (CONSUMERS)
1. **`IPaymentCompletedEvent`** (hợp đồng dùng chung; tài liệu cũ gọi là `OrderPaidEvent` — tên này **không tồn tại** trong registry hợp đồng):
   - **Xử lý (đặc tả mục tiêu, chưa triển khai)**: Tự động tạo bản ghi `Shipment` (Vận đơn), cấp mã `TrackingNumber` (tài liệu cũ ghi `TrackingCode` — tên cột thật là `TrackingNumber`, `Fulfillment.Domain/Entities/Shipment.cs:13`), chuyển trạng thái vận đơn sang `ReadyToPick` (tài liệu cũ ghi `Preparing` — **không có trong enum** `ShipmentStatus`).
   - **Phát hành tiếp (đặc tả mục tiêu)**: Bắn sự kiện `IShipmentCreatedEvent`.
| Consumer | Event | Trạng thái | Bằng chứng |
|---|---|---|---|
| — | `IPaymentCompletedEvent` | ❌ Chưa có — 0 class implement `IConsumer<>` | grep `IConsumer` toàn repo = 0 |
> Hệ quả thực tế: **không có gì kích hoạt việc tạo shipment**. Vận đơn duy nhất trong DB là dữ liệu seed (`Fulfillment.Infrastructure/Persistence/Seeds/FulfillmentSeedData.cs:116-137`).
---
## 2. SỰ KIỆN PHÁT HÀNH (EVENTS PUBLISHED)
| Tên Sự Kiện | Thời Điểm Kích Hoạt | Payload Dữ Liệu (đặc tả) | Services Quan Tâm | Trạng thái | Bằng chứng |
|:---|:---|:---|:---|:---|:---|
| `IShipmentCreatedEvent` (tài liệu cũ ghi `ShipmentCreatedEvent`) | Phiếu xuất kho & mã vận đơn được tạo | `OrderId`, `TrackingNumber`, `Carrier`, `EstimatedDelivery` | `CommerceService`, `NotificationService` | ❌ Chưa có — đặc tả mục tiêu, chưa triển khai | grep `IPublishEndpoint`/`.Publish(` = 0 |
| `ShipmentStatusUpdatedEvent` | Hành trình kiện hàng thay đổi vị trí/trạng thái | `TrackingNumber`, `Status`, `CurrentLocation`, `UpdatedAt` | `NotificationService`, `LogService` | ❌ Chưa có — đặc tả mục tiêu, chưa triển khai | 0 publisher; 0 writer cho `ShipmentEvent` |
| `ShipmentDeliveredEvent` | Khách nhận hàng thành công | `OrderId`, `TrackingNumber`, `DeliveredAt` | `CommerceService`, `WalletService` | ❌ Chưa có — đặc tả mục tiêu, chưa triển khai | 0 publisher |
**Tổng: 0/3 event đã publish.** Không có `IProducer`/`IPublishEndpoint` nào được inject vào bất kỳ class nào.
---
## 3. HẠ TẦNG MESSAGING ĐANG CÓ (để không nhầm là "không có gì")
`Fulfillment.Infrastructure/DependencyInjection.cs:23-48` có đăng ký thật:
| Thành phần | Trạng thái | Bằng chứng |
|---|---|---|
| `AddMassTransit` + `SetKebabCaseEndpointNameFormatter()` | ✅ | dòng 23-25 |
| `AddEntityFrameworkOutbox<AppDbContext>` với `UsePostgres()` + `UseBusOutbox()` | ✅ | dòng 27-31 |
| `UsingRabbitMq` (host/port/user/password lấy từ config, mặc định `localhost:5672`, `guest/guest`) | ✅ | dòng 33-47 |
| Endpoint/consumer được map | ❌ 0 endpoint, vì 0 consumer | `cfg.ConfigureEndpoints(context)` không tạo endpoint nào |
| Bảng outbox/inbox trong DB | ✅ 3 bảng (`InboxState`, `OutboxMessage`, `OutboxState`) | `Fulfillment.Infrastructure/Migrations/20261024103218_InitialFulfillmentSchema.cs` |
**Phiên bản package thật** (sửa lỗi tài liệu cũ ghi `8.4.1`):
| Package | Version | Bằng chứng |
|---|---|---|
| `MassTransit` | `8.3.0` | `Fulfillment.Infrastructure/Fulfillment.Infrastructure.csproj:22` |
| `MassTransit.RabbitMQ` | `8.3.0` | cùng file, dòng 23 |
| `MassTransit.EntityFrameworkCore` | `8.3.0` | cùng file, dòng 24 |