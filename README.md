# FULFILLMENT SERVICE — ARCHITECTURE & FOLDER STRUCTURE GUIDE
## (CẨM NANG CẤU TRÚC THƯ MỤC & PHÁT TRIỂN DÀNH CHO DEVELOPER VÀ AI AGENT)
> **Trạng thái triển khai (2026-10-24):** ❌ Chưa có bề mặt giao tiếp — **0/3 rpc** gRPC, **0 controller**, **0 consumer**, **0 publisher**; bề mặt duy nhất là `GET /health` (`Fulfillment.Api/Program.cs:57`). Toàn bộ tầng Application hiện **read-only**: **0 lời gọi `SaveChangesAsync`** trong code (chỉ có khai báo ở `Fulfillment.Application/Interfaces/IFulfillmentDbContext.cs:21`).
> **Nguồn sự thật:** code trong repo này (`file:line` ghi kèm trong từng bảng).
> **Cập nhật lần cuối:** 2026-10-24.
> **Thông tin dịch vụ**:
> - **Service Port (Docker/Compose)**: `8006` (xem `Dockerfile:3-4`, `docker-compose.yml` mục `fulfillment-service`).
> - **Service Port (chạy local bằng `dotnet run`)**: `http://localhost:5228`, `https://localhost:7014` (xem `Fulfillment.Api/Properties/launchSettings.json`) — **lệch với `8006`**, chi tiết ở §1.1.
> - **Database**: PostgreSQL (`fulfillment_db`), Port máy host: `5438`.
> - **Nhiệm vụ chính**: Quản lý kho bãi vật lý (Warehouses), Tồn kho thực tế (InventoryItem), Đơn vị vận chuyển (Carriers), Kiện hàng (Shipments) và Tra cứu vận đơn (Tracking).
> - **Hiện trạng code**: 46 file `.cs` / 1.184 LOC (không tính `Migrations/`), 10 entity ↔ 10 `DbSet` ↔ 10 configuration ↔ 13 bảng trong migration (10 bảng nghiệp vụ + 3 bảng outbox của MassTransit), **0 test**.
---
## 1. SƠ ĐỒ PHÂN TẦNG VÀ CẤU TRÚC THƯ MỤC CHUẨN
Chú thích trạng thái: ✅ có trong code · ⚠️ có khung nhưng thiếu phần lõi · ❌ không tồn tại (đặc tả mục tiêu).
```
src/Services/Fulfillment/
│
├── Fulfillment.Domain/                                # 1. TẦNG DOMAIN (POCO 100%, ZERO DEPENDENCY) ✅
│   ├── Base/                                          # ✅ BaseEntity.cs, IAuditable.cs (AuditableEntity), ISoftDelete.cs
│   ├── Entities/                                      # ✅ 10 entity: Warehouse, InventoryItem, InventoryReservation,
│   │                                                  #    InventoryReservationItem, InventoryLedger, Carrier, Shipment,
│   │                                                  #    ShipmentItem, ShipmentEvent, AuditLog
│   ├── Enums/                                         # ✅ 9 enum: ShipmentStatus, ShipmentEventStatus, ShippingServiceType,
│   │                                                  #    WarehouseType, InventoryMovementType, InventoryLedgerReferenceType,
│   │                                                  #    InventoryReservationStatus, InventoryReservationReferenceType, AuditAction
│   └── Exceptions/                                    # ✅ DomainException.cs (DomainException, EntityNotFoundException,
│                                                      #    BusinessRuleValidationException)
│
├── Fulfillment.Application/                           # 2. TẦNG APPLICATION (USE CASES & BUSINESS LOGIC) ⚠️ read-only
│   ├── Interfaces/                                    # ✅ Interfaces của CSDL
│   │   └── IFulfillmentDbContext.cs                   # ✅ 10 DbSet + SaveChangesAsync (chỉ khai báo, 0 lời gọi)
│   │
│   ├── Common/                                        # ❌ KHÔNG tồn tại trong code (đặc tả mục tiêu)
│   │
│   ├── Services/                                      # ✅ CÁC DỊCH VỤ XỬ LÝ USE CASE (THEO TÍNH NĂNG)
│   │   ├── Shipment/                                  # Module Vận đơn & Giao hàng
│   │   │   ├── IShipmentService.cs                    # ✅ GetByTrackingNumberAsync, GetByOrderIdAsync (chỉ đọc)
│   │   │   ├── ShipmentService.cs                     # ✅ 2 truy vấn AsNoTracking; block map 10 dòng lặp lại 2 lần (dòng 26-35 và 46-55)
│   │   │   └── ShipmentDtos.cs                        # ✅ ShipmentDto; ❌ CreateShipmentDto (khai báo nhưng 0 nơi dùng)
│   │   └── Warehouse/                                 # Module Kho bãi vật lý
│   │       ├── IWarehouseService.cs                   # ✅ GetByIdAsync, ListActiveAsync (chỉ đọc)
│   │       ├── WarehouseService.cs                    # ✅ 2 truy vấn AsNoTracking
│   │       └── WarehouseDtos.cs                       # ✅ WarehouseDto
│   │
│   └── DependencyInjection.cs                         # ✅ Đăng ký AddApplicationServices() (2 service)
│
├── Fulfillment.Infrastructure/                        # 3. TẦNG INFRASTRUCTURE (CÔNG NGHỆ & HẠ TẦNG CỤ THỂ) ⚠️
│   ├── Persistence/                                   # ✅ Quản lý CSDL PostgreSQL & EF Core 8.0.10
│   │   ├── AppDbContext.cs                            # ✅ Triển khai IFulfillmentDbContext; ⚠️ kèm FulfillmentDbContext
│   │   │                                              #    là lớp con rỗng, trùng lặp 100% hành vi (dòng 38-43)
│   │   ├── Configurations/                            # ✅ 10 Fluent API Configurations (*Configuration.cs)
│   │   ├── Migrations/                                # ✅ 1 migration: 20261024103218_InitialFulfillmentSchema (13 bảng)
│   │   └── Seeds/                                     # ✅ FulfillmentSeedData.cs (1 kho, 1 carrier, 5 InventoryItem,
│   │                                                  #    1 Shipment, 1 ShipmentItem)
│   │
│   └── DependencyInjection.cs                         # ✅ AddInfrastructureServices(configuration): DbContext +
│                                                      #    MassTransit 8.3.0 + RabbitMQ + EF Outbox (⚠️ 0 message flow)
│
└── Fulfillment.Api/                                   # 4. TẦNG API / HOST (ENTRY POINT & MIDDLEWARE) ⚠️
    ├── Middleware/                                    # ✅ MIDDLEWARE TOÀN CỤC CỦA SERVICE
    │   └── GlobalExceptionHandlerMiddleware.cs        # ✅ Bắt mọi lỗi ngoại lệ, format RFC 9110 ProblemDetails
    ├── Services/                                      # ❌ KHÔNG tồn tại — đặc tả cũ ghi FulfillmentGrpcService.cs
    ├── Contracts/                                     # ❌ KHÔNG tồn tại — đặc tả cũ ghi shop.proto
    └── Program.cs                                     # ✅ Khởi động Kestrel HTTP/1 + HTTP/2 (h2c), Swagger (Development),
                                                       #    tự động Migrate() khi khởi động, 1 endpoint GET /health (dòng 57)
```
### 1.1. Port thật — đặc tả vs. chạy local
| Ngữ cảnh | Port | Bằng chứng | Trạng thái |
|---|---|---|---|
| Docker / `docker-compose` | `8006` | `Dockerfile:3-4` (`EXPOSE 8006`, `ASPNETCORE_URLS=http://+:8006`), `docker-compose.yml` (`8006:8006`) | ✅ |
| Chạy local `dotnet run` (profile `http`) | `5228` | `Fulfillment.Api/Properties/launchSettings.json` | ⚠️ lệch đặc tả `8006` |
| Chạy local `dotnet run` (profile `https`) | `7014` (https) + `5228` (http) | `Fulfillment.Api/Properties/launchSettings.json` | ⚠️ lệch đặc tả `8006` |
> Kết luận: đặc tả "port `8006`" chỉ đúng trong ngữ cảnh Docker. Chạy local mặc định là `5228`/`7014`; muốn đúng `8006` phải set `ASPNETCORE_URLS=http://localhost:8006`.
---
## 2. NGUYÊN TẮC PHÂN CHIA TRÁCH NHIỆM
1. **`Fulfillment.Domain`**: Chứa các Entity vận đơn, kho hàng, nhà vận chuyển, giao nhận. Tuyệt đối không import thư viện ngoài hay EF Core vào đây. — ✅ Đúng trong code (`Fulfillment.Domain.csproj` có 0 `PackageReference`).
2. **`Fulfillment.Application`**: Chứa toàn bộ ca sử dụng (Use Cases). Mỗi tính năng nằm trong thư mục riêng `Services/{Feature}/` gồm DTOs, Interface và Implementation. Gọi CSDL qua `IFulfillmentDbContext`. — ⚠️ Đúng về cấu trúc, nhưng hiện chỉ có **truy vấn đọc** (`AsNoTracking`), chưa có use case ghi nào.
3. **`Fulfillment.Infrastructure`**: Triển khai `AppDbContext` kế thừa `IFulfillmentDbContext`. Quản lý Transactional Outbox với MassTransit RabbitMQ. — ⚠️ Outbox **đã đăng ký** (`Fulfillment.Infrastructure/DependencyInjection.cs:27-31`) nhưng **0 consumer / 0 publisher** nên chưa có message nào đi qua outbox.
4. **`Fulfillment.Api`**: Cổng tiếp nhận cực kỳ mỏng. Trang bị `GlobalExceptionHandlerMiddleware` để chuẩn hóa lỗi trả về cho client. — ✅ Middleware có thật (`Fulfillment.Api/Middleware/GlobalExceptionHandlerMiddleware.cs`), nhưng hiện chỉ có 1 endpoint `/health` để áp dụng.
---
## 3. QUY TRÌNH THÊM TÍNH NĂNG MỚI (DÀNH CHO DEV & AI AGENT)
1. **Bước 1**: Tạo Entity & Enum trong `Fulfillment.Domain/Entities/`.
2. **Bước 2**: Tạo Configuration trong `Fulfillment.Infrastructure/Persistence/Configurations/` và khai báo `DbSet<T>` trong `IFulfillmentDbContext` & `AppDbContext`.
3. **Bước 3**: Tạo thư mục tính năng trong `Fulfillment.Application/Services/{Feature}/` gồm `I{Feature}Service.cs`, `{Feature}Service.cs`, và `{Feature}Dtos.cs`.
4. **Bước 4**: Đăng ký Service vào `Fulfillment.Application/DependencyInjection.cs`.
5. **Bước 5**: ❌ **Chưa có điểm nối bề mặt**: đặc tả cũ ghi "gọi Service trong `Fulfillment.Api/Services/FulfillmentGrpcService.cs`" nhưng file/thư mục này **không tồn tại**. Hiện chỉ có 2 lựa chọn hợp lệ: (a) thêm minimal endpoint trong `Program.cs`, hoặc (b) thêm controller + `AddControllers()` — cả hai đều chưa có. Xem `specs/02_API_GRPC_CONTRACTS.md` (0/3 rpc).
---
## 4. QUY CHUẨN THÔNG BÁO LỖI (MANDATORY ERROR RULES)
1. **100% English**: Tuyệt đối không dùng tiếng Việt.
2. **Sentence case**: Viết hoa duy nhất chữ cái đầu (`"Shipment not found with the provided tracking number."`).
3. **Error Code SCREAMING_SNAKE_CASE**: Ví dụ `SHIPMENT_NOT_FOUND`, `WAREHOUSE_INACTIVE`, `VAL_INVALID_INPUT`.
Hiện trạng mã lỗi đang dùng thật trong `Fulfillment.Api/Middleware/GlobalExceptionHandlerMiddleware.cs:36-63`: `NOT_FOUND`, `BUSINESS_RULE_VIOLATION`, `DOMAIN_ERROR`, `VAL_INVALID_ARGUMENT`, `INTERNAL_SERVER_ERROR`. Các mã `SHIPMENT_NOT_FOUND`, `WAREHOUSE_INACTIVE` **❌ chưa được dùng ở đâu** (đặc tả mục tiêu).
---
## 5. CÁCH CHẠY VÀ KIỂM THỬ ĐỘC LẬP SERVICE
```bash
# 1. Bật database của Fulfillment Service (PostgreSQL, Port 5438):
cd src/Services/Fulfillment
docker compose up -d fulfillment-db
# 2. Migration: ĐÃ có sẵn 1 migration trong repo
#    (Fulfillment.Infrastructure/Migrations/20261024103218_InitialFulfillmentSchema.cs)
#    => chỉ cần cập nhật DB, KHÔNG cần tạo lại migration.
#    Sửa lỗi tài liệu cũ: KHÔNG có thư mục ./.tools/dotnet-ef trong repo.
dotnet ef database update --project Fulfillment.Infrastructure --startup-project Fulfillment.Api
# 3. Chạy service (local mặc định 5228, xem §1.1):
dotnet run --project Fulfillment.Api
#    Muốn đúng port đặc tả 8006:
ASPNETCORE_URLS=http://localhost:8006 dotnet run --project Fulfillment.Api
# 4. Kiểm tra bề mặt duy nhất hiện có:
curl http://localhost:5228/health
# => {"status":"Healthy","service":"Fulfillment.Api"}
```
> ⚠️ Chưa có project test nào trong `Fulfillment.sln` (0 file test, 0 `[Fact]`/`[Theory]`), nên `dotnet test` hiện không kiểm chứng được gì. Xem `specs/05_DEV_VERIFICATION_CHECKLIST.md`.
---
## 6. NHỮNG ĐIỀU TÀI LIỆU CŨ NÓI SAI (đã sửa trong bản này)
| Tuyên bố cũ | Sự thật trong code | Bằng chứng |
|---|---|---|
| "Service Port `8006` (HTTP/1.1 REST & HTTP/2 gRPC)" | Kestrel bật `Http1AndHttp2` nhưng **0 gRPC service được map**; local chạy `5228`/`7014` | `Fulfillment.Api/Program.cs:10-16`, `Properties/launchSettings.json` |
| `Fulfillment.Api/Services/FulfillmentGrpcService.cs` | Thư mục `Services/` không tồn tại | `ls Fulfillment.Api/` → chỉ có `Middleware/`, `Program.cs` |
| `Fulfillment.Api/Contracts/shop.proto` | Thư mục `Contracts/` không tồn tại | `ls Fulfillment.Api/` |
| `Fulfillment.Application/Common/` | Thư mục không tồn tại | `ls Fulfillment.Application/` → `Interfaces/`, `Services/` |
| `./.tools/dotnet-ef` | Không tồn tại; dùng `dotnet ef` | `ls -a` tại repo root |
| `CreateShipmentDto` là DTO dùng để tạo vận đơn | Được khai báo nhưng **0 nơi tham chiếu** | `Fulfillment.Application/Services/Shipment/ShipmentDtos.cs:16-22` |