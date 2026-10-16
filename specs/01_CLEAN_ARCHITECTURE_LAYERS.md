# 01 — PHÂN TẦNG CLEAN ARCHITECTURE & QUY TẮC PHỤ THUỘC (FULFILLMENT SERVICE)

Solution `Fulfillment.sln` bao gồm 4 project tương ứng với 4 tầng kiến trúc sạch (Clean Architecture):

```
Fulfillment.sln
├── Fulfillment.Domain/           # Entities (Warehouse, StockItem, Shipment), Value Objects (Address, Weight)
├── Fulfillment.Application/      # Use cases CQRS (CalculateShippingFeeQuery, CreateShipmentCommand)
├── Fulfillment.Infrastructure/   # EF Core DbContext, PostgreSQL mappings, 3rd Party Carrier Integration
└── Fulfillment.Api/              # Điểm khởi chạy gRPC Server (Program.cs, DI setup)
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
- **`Fulfillment.Domain`**: Chứa Entity `Shipment`, `ShipmentHistory`, `WarehouseItem`. Độc lập 100% không phụ thuộc project ngoài.
- **`Fulfillment.Application`**: Chứa thuật toán tính toán cước vận chuyển, các ca sử dụng tạo vận đơn, cập nhật trạng thái giao hàng.
- **`Fulfillment.Infrastructure`**: Chứa `FulfillmentDbContext`, cấu hình bảng PostgreSQL, và các Service tích hợp nhà vận chuyển bên ngoài (nếu có).
- **`Fulfillment.Api`**: Điểm khởi động gRPC Server, đăng ký `app.MapGrpcService<FulfillmentGrpcService>()`.
