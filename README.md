# FULFILLMENT SERVICE — ARCHITECTURE & FOLDER STRUCTURE GUIDE
## (CẨM NANG CẤU TRÚC THƯ MỤC & PHÁT TRIỂN DÀNH CHO DEVELOPER VÀ AI AGENT)

> **Thông tin dịch vụ**:
> - **Service Port**: `8006` (HTTP/1.1 REST & HTTP/2 gRPC)
> - **Database**: PostgreSQL (`fulfillment_db`), Port máy host: `5438`
> - **Nhiệm vụ chính**: Quản lý kho bãi vật lý (Warehouses), Tồn kho thực tế (InventoryItem), Đơn vị vận chuyển (Carriers), Kiện hàng (Shipments) và Tra cứu vận đơn (Tracking).

---

## 1. SƠ ĐỒ PHÂN TẦNG VÀ CẤU TRÚC THƯ MỤC CHUẨN

```
src/Services/Fulfillment/
│
├── Fulfillment.Domain/                                # 1. TẦNG DOMAIN (POCO 100%, ZERO DEPENDENCY)
│   ├── Base/                                          # BaseEntity, IAuditable, ISoftDelete
│   ├── Entities/                                      # Shipment, Warehouse, Carrier, InventoryItem...
│   ├── Enums/                                         # ShipmentStatus, ShippingServiceType, WarehouseType...
│   └── Exceptions/                                    # DomainException, BusinessRuleValidationException
│
├── Fulfillment.Application/                           # 2. TẦNG APPLICATION (USE CASES & BUSINESS LOGIC)
│   ├── Interfaces/                                    # Interfaces của CSDL và các dịch vụ hạ tầng ngoài
│   │   └── IFulfillmentDbContext.cs                   # Abstraction của DbContext
│   │
│   ├── Common/                                        # Tiện ích và models dùng chung trong Application
│   │
│   ├── Services/                                      # CÁC DỊCH VỤ XỬ LÝ USE CASE (THEO TÍNH NĂNG)
│   │   ├── Shipment/                                  # Module Vận đơn & Giao hàng
│   │   │   ├── IShipmentService.cs                    # Interface ca sử dụng vận đơn
│   │   │   ├── ShipmentService.cs                     # Xử lý nghiệp vụ vận đơn
│   │   │   └── ShipmentDtos.cs                        # ShipmentDto, CreateShipmentDto...
│   │   └── Warehouse/                                 # Module Kho bãi vật lý
│   │       ├── IWarehouseService.cs                   # Interface ca sử dụng kho bãi
│   │       ├── WarehouseService.cs                    # Xử lý nghiệp vụ kho bãi
│   │       └── WarehouseDtos.cs                       # WarehouseDto...
│   │
│   └── DependencyInjection.cs                         # Đăng ký AddApplicationServices()
│
├── Fulfillment.Infrastructure/                        # 3. TẦNG INFRASTRUCTURE (CÔNG NGHỆ & HẠ TẦNG CỤ THỂ)
│   ├── Persistence/                                   # Quản lý CSDL PostgreSQL & EF Core 8
│   │   ├── AppDbContext.cs                            # Triển khai IFulfillmentDbContext
│   │   ├── Configurations/                            # 10 Fluent API Configurations (*Configuration.cs)
│   │   ├── Migrations/                                # Code-first Database Migrations
│   │   └── Seeds/                                     # Dữ liệu hạt giống kho bãi & đơn vị vận chuyển
│   │
│   └── DependencyInjection.cs                         # Đăng ký AddInfrastructureServices(configuration)
│
└── Fulfillment.Api/                                   # 4. TẦNG API / HOST (ENTRY POINT & MIDDLEWARE)
    ├── Middleware/                                    # MIDDLEWARE TOÀN CỤC CỦA SERVICE
    │   └── GlobalExceptionHandlerMiddleware.cs        # Bắt mọi lỗi ngoại lệ, format RFC 9110 ProblemDetails
    ├── Services/                                      # gRPC Server Service (FulfillmentGrpcService.cs)
    ├── Contracts/                                     # shop.proto (Hợp đồng gRPC)
    └── Program.cs                                     # Khởi động Kestrel HTTP/1 & HTTP/2, gọi DI App & Infra
```

---

## 2. NGUYÊN TẮC PHÂN CHIA TRÁCH NHIỆM

1. **`Fulfillment.Domain`**: Chứa các Entity vận đơn, kho hàng, nhà vận chuyển, giao nhận. Tuyệt đối không import thư viện ngoài hay EF Core vào đây.
2. **`Fulfillment.Application`**: Chứa toàn bộ ca sử dụng (Use Cases). Mỗi tính năng nằm trong thư mục riêng `Services/{Feature}/` gồm DTOs, Interface và Implementation. Gọi CSDL qua `IFulfillmentDbContext`.
3. **`Fulfillment.Infrastructure`**: Triển khai `AppDbContext` kế thừa `IFulfillmentDbContext`. Quản lý Transactional Outbox với MassTransit RabbitMQ.
4. **`Fulfillment.Api`**: Cổng tiếp nhận cực kỳ mỏng. Trang bị `GlobalExceptionHandlerMiddleware` để chuẩn hóa lỗi trả về cho client.

---

## 3. QUY TRÌNH THÊM TÍNH NĂNG MỚI (DÀNH CHO DEV & AI AGENT)

1. **Bước 1**: Tạo Entity & Enum trong `Fulfillment.Domain/Entities/`.
2. **Bước 2**: Tạo Configuration trong `Fulfillment.Infrastructure/Persistence/Configurations/` và khai báo `DbSet<T>` trong `IFulfillmentDbContext` & `AppDbContext`.
3. **Bước 3**: Tạo thư mục tính năng trong `Fulfillment.Application/Services/{Feature}/` gồm `I{Feature}Service.cs`, `{Feature}Service.cs`, và `{Feature}Dtos.cs`.
4. **Bước 4**: Đăng ký Service vào `Fulfillment.Application/DependencyInjection.cs`.
5. **Bước 5**: Gọi Service trong `Fulfillment.Api/Services/FulfillmentGrpcService.cs`.

---

## 4. QUY CHUẨN THÔNG BÁO LỖI (MANDATORY ERROR RULES)

1. **100% English**: Tuyệt đối không dùng tiếng Việt.
2. **Sentence case**: Viết hoa duy nhất chữ cái đầu (`"Shipment not found with the provided tracking number."`).
3. **Error Code SCREAMING_SNAKE_CASE**: Ví dụ `SHIPMENT_NOT_FOUND`, `WAREHOUSE_INACTIVE`, `VAL_INVALID_INPUT`.

---

## 5. CÁCH CHẠY VÀ KIỂM THỬ ĐỘC LẬP SERVICE

```bash
# 1. Bật database của Fulfillment Service (Port 5438):
cd src/Services/Fulfillment
docker compose up -d fulfillment-db

# 2. Tạo & Cập nhật migration:
./.tools/dotnet-ef migrations add InitialFulfillmentSchema --project Fulfillment.Infrastructure --startup-project Fulfillment.Api
./.tools/dotnet-ef database update --project Fulfillment.Infrastructure --startup-project Fulfillment.Api

# 3. Chạy service:
dotnet run --project Fulfillment.Api
```
