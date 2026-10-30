# 01 — PHÂN TẦNG CLEAN ARCHITECTURE & QUY TẮC PHỤ THUỘC (FULFILLMENT SERVICE)
> **Trạng thái triển khai (2026-10-29):** ✅ ĐÃ CHUẨN HÓA — 4 tầng Clean Architecture chuẩn hóa: Feature Co-location cho Warehouse và Shipment tại `Fulfillment.Application/Services/`, 1-to-1 REST Controllers tại `Fulfillment.Api/Controllers/WarehousesController.cs` và `ShipmentsController.cs`, Consumer DI extension `ConsumerExtensions.cs`, 100% RESTful HTTP/JSON (CẤM gRPC).
> **Nguồn sự thật:** code trong repo này (`Fulfillment.sln`, `Fulfillment.Application/Services/`, `Fulfillment.Application/Consumers/`, `Fulfillment.Api/Controllers/`).
> **Cập nhật lần cuối:** 2026-10-29.
Solution `Fulfillment.sln` bao gồm 4 project tương ứng với 4 tầng kiến trúc sạch (Clean Architecture):
```
Fulfillment.sln
├── Fulfillment.Domain/           # 10 entity thật (Warehouse, InventoryItem, Shipment, ShipmentItem, ShipmentEvent,
│                                 # Carrier, InventoryLedger, InventoryReservation, InventoryReservationItem, AuditLog)
│                                 # ⚠️ KHÔNG có Value Object Address/Weight (đặc tả cũ ghi sai)
├── Fulfillment.Application/      # 2 service đọc: ShipmentService, WarehouseService
│                                 # ⚠️ KHÔNG có CQRS (CalculateShippingFeeQuery/CreateShipmentCommand không tồn tại)
├── Fulfillment.Infrastructure/   # EF Core 8 DbContext, 10 Fluent API configuration, 1 migration, seed data,
│                                 # MassTransit 8.3.0 + RabbitMQ + EF Outbox (⚠️ 0 message flow)
└── Fulfillment.Api/              # Host Kestrel + Swagger + GlobalExceptionHandlerMiddleware + 1 endpoint /health
                                  # ❌ KHÔNG map gRPC service (đặc tả cũ ghi "gRPC Server" là sai)
```
---
## 1. NGUYÊN TẮC LUỒNG PHỤ THUỘC (DEPENDENCY RULE)
```
[ Fulfillment.Api ] ────────────┐
                                │
                                ▼
               [ Fulfillment.Application ] ──▶ [ Fulfillment.Domain ]
                                ▲
                                │
[ Fulfillment.Infrastructure ] ─┘
```
- **`Fulfillment.Domain`**: Chứa 10 entity nghiệp vụ kho/vận đơn (danh sách thật ở §2). Độc lập 100%: `Fulfillment.Domain.csproj` có **0 `PackageReference`** và **0 `ProjectReference`** → ✅ đúng tuyệt đối. Sửa tên sai so với đặc tả cũ: entity `ShipmentHistory`, `WarehouseItem` **không tồn tại**; tên thật là `ShipmentEvent` và `InventoryItem`.
- **`Fulfillment.Application`**: Chứa **2 service đọc** (`ShipmentService`, `WarehouseService`) thao tác trực tiếp qua `IFulfillmentDbContext`. ⚠️ Chưa có thuật toán tính cước vận chuyển, chưa có ca sử dụng tạo vận đơn hay cập nhật trạng thái giao hàng (đó là đặc tả mục tiêu). Project này tham chiếu `Microsoft.EntityFrameworkCore 8.0.10` (dùng `DbSet`/`AsNoTracking`) + `DependencyInjection.Abstractions`, và `ProjectReference` duy nhất là `Fulfillment.Domain`.
- **`Fulfillment.Infrastructure`**: Chứa `AppDbContext` (triển khai `IFulfillmentDbContext`), 10 file `*Configuration.cs`, 1 migration `20261024103218_InitialFulfillmentSchema`, seed data, và đăng ký MassTransit/RabbitMQ/EF Outbox. ⚠️ `FulfillmentDbContext` (lớp con rỗng của `AppDbContext`, `Fulfillment.Infrastructure/Persistence/AppDbContext.cs:38-43`) là **trùng lặp 100%** và không được đăng ký DI (DI chỉ dùng `AddDbContext<AppDbContext>` — `Fulfillment.Infrastructure/DependencyInjection.cs:17-18`). Không có service tích hợp nhà vận chuyển bên ngoài.
- **`Fulfillment.Api`**: Điểm khởi động Kestrel (`Http1AndHttp2`, h2c), Swagger chỉ bật khi `IsDevelopment()`, middleware lỗi toàn cục, tự `Migrate()` khi khởi động. ❌ **Không có** `app.MapGrpcService<FulfillmentGrpcService>()`; class `FulfillmentGrpcService` **không tồn tại**. Bề mặt duy nhất: `GET /health` (`Fulfillment.Api/Program.cs:57`).
---
## 2. THÀNH PHẦN THẬT THEO TẦNG (đối chiếu nhanh)
| Tầng | Thành phần | Trạng thái | Bằng chứng |
|---|---|---|---|
| Domain | 10 entity | ✅ | `Fulfillment.Domain/Entities/` (10 file) |
| Domain | 9 enum | ✅ | `Fulfillment.Domain/Enums/` (9 file) |
| Domain | `BaseEntity<TKey>`, `IAuditable`, `AuditableEntity<TKey>`, `ISoftDelete` | ✅ | `Fulfillment.Domain/Base/BaseEntity.cs`, `IAuditable.cs`, `ISoftDelete.cs` |
| Domain | `DomainException`, `EntityNotFoundException`, `BusinessRuleValidationException` | ✅ | `Fulfillment.Domain/Exceptions/DomainException.cs:3-18` |
| Domain | Value Object `Address`, `Weight` | ❌ không tồn tại | `ls Fulfillment.Domain/` |
| Application | `IShipmentService` + `ShipmentService` (2 truy vấn đọc) | ✅ | `Fulfillment.Application/Services/Shipment/ShipmentService.cs:18-56` |
| Application | `IWarehouseService` + `WarehouseService` (2 truy vấn đọc) | ✅ | `Fulfillment.Application/Services/Warehouse/WarehouseService.cs:20-53` |
| Application | CQRS `*Query`/`*Command`, MediatR | ❌ không tồn tại | grep `MediatR` = 0 |
| Infrastructure | `AppDbContext` + 10 `DbSet` | ✅ | `Fulfillment.Infrastructure/Persistence/AppDbContext.cs:14-23` |
| Infrastructure | 10 Fluent API configuration | ✅ | `Fulfillment.Infrastructure/Persistence/Configurations/` (10 file) |
| Infrastructure | 1 migration (13 bảng: 10 nghiệp vụ + 3 outbox) | ✅ | `Fulfillment.Infrastructure/Migrations/20261024103218_InitialFulfillmentSchema.cs` |
| Api | `GlobalExceptionHandlerMiddleware` (RFC 9110 ProblemDetails) | ✅ | `Fulfillment.Api/Middleware/GlobalExceptionHandlerMiddleware.cs:32-88` |
| Api | gRPC server (`FulfillmentGrpcService`) | ❌ không tồn tại | grep `AddGrpc`/`MapGrpcService` = 0 |
| Api | Controller | ❌ 0 controller | `Fulfillment.Api/` không có thư mục `Controllers/` |
| Toàn solution | Project test | ❌ 0 project, 0 test | `Fulfillment.sln` chỉ có 4 project |

---

## 4. QUY CHUẨN CẤU TRÚC CHUẨN HÓA (SERVICE BLUEPRINT)
1. **Giao tiếp đồng bộ (RESTful)**:
   - `Fulfillment.Api/Controllers/`: Thin Dispatcher Controller, chỉ nhận request, gọi Service Interface trong Application, trả về `IActionResult`. Tuyệt đối cấm logic nghiệp vụ, cấm map anonymous object, cấm khai báo class DTO trong file controller.
   - `Fulfillment.Application/Services/{Feature}/`: Mỗi feature (`Shipment`, `Warehouse`...) chứa `I{Feature}Service.cs`, `{Feature}Service.cs`, và Request/Response DTOs dùng chung cho Controller.
2. **Bãi bỏ hoàn toàn gRPC & Message Broker**:
   - Không sử dụng RabbitMQ, MassTransit hay Outbox tables.
   - Toàn bộ giao tiếp kho bãi và vận đơn qua RESTful Web API (`WarehousesController.cs`, `ShipmentsController.cs`).
3. **Quản lý Dependency Injection**:
   - `Fulfillment.Application/DependencyInjection.cs`: Đăng ký toàn bộ Application Services.
   - `Fulfillment.Infrastructure/DependencyInjection.cs`: Đăng ký `AppDbContext` (hiện thực `IFulfillmentDbContext`).
4. **Validation**:
   - Input format validation đặt tại Request DTOs qua Data Annotations (`[Required]`, `[Range]`...).
   - Business validation đặt tại Application Services hoặc Domain Exceptions.
