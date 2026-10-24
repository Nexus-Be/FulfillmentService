# FULFILLMENT SERVICE SPEC KIT — CHỈ DẪN KỸ THUẬT CHO AI AGENT & DEVELOPER
> **Trạng thái triển khai (2026-10-24):** ❌ Chưa có bề mặt — **0/3 rpc** gRPC, **0/3 event** publish, **0/1 event** consume, **0 controller**, **0 test**; duy nhất `GET /health` (`Fulfillment.Api/Program.cs:57`). Toàn bộ service **read-only** (0 lời gọi `SaveChangesAsync`).
> **Nguồn sự thật:** code trong repo này (`file:line` ghi kèm trong từng bảng).
> **Cập nhật lần cuối:** 2026-10-24.
Tài liệu này là **Bộ Chỉ Dẫn & Đặc Tả Kỹ Thuật (Spec Kit)** cho repository **FulfillmentService** thuộc hệ sinh thái Microservices **Nexus**. Giúp lập trình viên và các AI Coding Agent (Cursor, Claude, Copilot, DeepSeek...) nắm bắt bối cảnh quản lý kho bãi vật lý, tính phí giao vận, theo dõi hành trình kiện hàng và **loại bỏ hoàn toàn nguy cơ ảo giác (zero hallucination)**.
---
## 1. MỤC LỤC BỘ ĐẶC TẢ FULFILLMENT SERVICE (SPECS/)
| File Đặc Tả | Trọng Tâm Nội Dung | Trạng thái | Bằng chứng |
|:---|:---|:---|:---|
| [00_AGENT_MISSION_GUARDRAILS.md](00_AGENT_MISSION_GUARDRAILS.md) | Bounded Context, Port `8006` (Docker) / `5228` (local), DB `fulfillment_db` (`5438`), các điều cấm kỵ cốt lõi. | ⚠️ Một phần — ranh giới kho đúng, nhưng 5 ca sử dụng mô tả là đặc tả mục tiêu | `Fulfillment.Api/Program.cs:57` |
| [01_CLEAN_ARCHITECTURE_LAYERS.md](01_CLEAN_ARCHITECTURE_LAYERS.md) | Cấu trúc 4 tầng Clean Architecture (`.Api`, `.Application`, `.Domain`, `.Infrastructure`), luồng phụ thuộc. | ⚠️ Một phần — 4 project đúng, nhưng 0 CQRS, 0 Value Object, 0 gRPC service | `Fulfillment.sln`, `Fulfillment.Application/Services/` |
| [02_API_GRPC_CONTRACTS.md](02_API_GRPC_CONTRACTS.md) | Hợp đồng gRPC `FulfillmentApi`, tính phí ship, tra cứu vận đơn. | ❌ Chưa có — 0/3 rpc, không có `shop.proto` trong repo | grep `MapGrpcService` = 0 |
| [03_EVENT_MESSAGING.md](03_EVENT_MESSAGING.md) | Sự kiện RabbitMQ + MassTransit **8.3.0** (Nhận `IPaymentCompletedEvent` → Phát hành `IShipmentCreatedEvent`). | ❌ Chưa có — 0 consumer, 0 publisher (chỉ đăng ký bus + outbox) | grep `IConsumer`/`IPublishEndpoint` = 0 |
| [04_DATA_PERSISTENCE.md](04_DATA_PERSISTENCE.md) | Mô hình thực thể `Warehouse`, `InventoryItem`, `Shipment`, DbContext EF Core 8 (No Repository Pattern). | ⚠️ Một phần — 10 entity/10 bảng đầy đủ, nhưng 5 entity không có writer | `Fulfillment.Infrastructure/Persistence/AppDbContext.cs:14-23` |
| [05_DEV_VERIFICATION_CHECKLIST.md](05_DEV_VERIFICATION_CHECKLIST.md) | Lệnh biên dịch, kiểm thử tự động, checklist kiểm tra trước khi commit. | ⚠️ Một phần — build được, không có test để chạy | không có project `*Tests*` |
---
## 2. THÔNG SỐ VẬN HÀNH CHÍNH
| Thông số | Giá trị đặc tả (Docker) | Giá trị thật khi chạy local | Trạng thái | Bằng chứng |
|:---|:---|:---|:---|:---|
| Ngôn ngữ & Runtime | .NET 8 (C# 12) | .NET 8 (`net8.0`) | ✅ | `Fulfillment.Api/Fulfillment.Api.csproj` |
| Cổng service | `8006` | `http=5228`, `https=7014` | ⚠️ lệch | `Dockerfile:3-4` vs `Fulfillment.Api/Properties/launchSettings.json` |
| Cơ sở dữ liệu | PostgreSQL `5438`, DB `fulfillment_db` | `5438` (host) | ✅ | `docker-compose.yml` (`fulfillment-db`) |
| Message broker | RabbitMQ `5672`, vhost `/` | `5672`, vhost `/` | ⚠️ đã đăng ký, 0 message | `Fulfillment.Infrastructure/DependencyInjection.cs:33-47` |
| MassTransit | phiên bản **8.3.0** (tài liệu cũ ghi 8.4.1 — sai) | `8.3.0` | ✅ | `Fulfillment.Infrastructure/Fulfillment.Infrastructure.csproj:22-24` |
| Bề mặt REST | "mọi REST call qua ApiGateway" | chỉ `GET /health` trực tiếp trên service | ❌ | `Fulfillment.Api/Program.cs:57` |
| Bề mặt gRPC | `FulfillmentApi` với 3 rpc | **0 rpc** — không có `MapGrpcService` | ❌ | grep `AddGrpc`/`MapGrpcService` = 0 |
---
## 3. CÁCH ĐỌC BỘ SPEC NÀY
1. Mỗi tuyên bố kỹ thuật mang một nhãn: **✅ CÓ** (đã có trong code, kèm `file:line`) · **⚠️ MỘT PHẦN** (có khung, thiếu lõi) · **❌ CHƯA CÓ** (chỉ là đặc tả mục tiêu).
2. Phần thiết kế gốc **không bị xoá** — nó là target. Chỗ nào chưa triển khai sẽ ghi rõ "đặc tả mục tiêu, chưa triển khai".
3. Khi code và spec xung đột: **code là sự thật**, spec phải sửa theo code (không sửa ngược).