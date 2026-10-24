# 04 — MÔ HÌNH THỰC THỂ & CƠ SỞ DỮ LIỆU (FULFILLMENT SERVICE DATA PERSISTENCE)
> **Trạng thái triển khai (2026-10-24):** ⚠️ Một phần — schema đầy đủ **10/10 bảng nghiệp vụ** (+3 bảng outbox của MassTransit), **10/10 entity** ↔ **10/10 `DbSet`** ↔ **10/10 configuration**, nhưng **5/10 entity không có writer** (`InventoryLedger`, `InventoryReservation`, `InventoryReservationItem`, `ShipmentEvent`, `AuditLog`) và toàn repo có **0 lời gọi `SaveChangesAsync`** ⇒ chưa ghi được gì ở runtime. **0 transaction, 0 lock, 0 optimistic concurrency, 0 CHECK constraint.**
> **Nguồn sự thật:** code trong repo này (`file:line` ghi kèm trong từng bảng).
> **Cập nhật lần cuối:** 2026-10-24.
---
## 1. MÔ HÌNH THỰC THỂ CHÍNH (DOMAIN ENTITIES)
Tất cả entity (trừ `AuditLog`) kế thừa `BaseEntity<Guid>` (`Fulfillment.Domain/Base/BaseEntity.cs`) gồm `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`; `Warehouse` và `Shipment` dùng `AuditableEntity<Guid>` (thêm `CreatedBy`, `UpdatedBy`). `AuditLog` **không** kế thừa base (chỉ có `Id` + `CreatedAt`, không có `UpdatedAt`/`IsDeleted`).
### 1.1. `Warehouse` (Kho bãi) — `Fulfillment.Domain/Entities/Warehouse.cs`
| Thuộc tính | Kiểu | Ghi chú | Bằng chứng |
|---|---|---|---|
| `Id` | `Guid` | PK | `Warehouse.cs:6` |
| `Code` | `string` | Max 100, **Unique Index** | `WarehouseConfiguration.cs:12-13` |
| `Name` | `string` | Max 255, required | `WarehouseConfiguration.cs:14` |
| `Type` | `WarehouseType` | Lưu dạng **string** trong DB | `WarehouseConfiguration.cs:15` |
| `SellerId` | `Guid?` | Có index; cross-service ref → user id | `WarehouseConfiguration.cs:16-17` |
| `Address` | `string?` | Max 1000 | `WarehouseConfiguration.cs:18` |
| `IsDisabled` | `bool` | — | `Warehouse.cs:13` |
| `CreatedBy`, `UpdatedBy` | `Guid?` | Từ `AuditableEntity` — tài liệu cũ bỏ sót | `Fulfillment.Domain/Base/IAuditable.cs:20-25` |
- Navigation: `ICollection<InventoryItem> InventoryItems`, `ICollection<Shipment> Shipments` — ✅.
- Trạng thái: ⚠️ Có seed 1 kho (`WH-CENTRAL-01`, id `eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee`); **0 writer** runtime.
### 1.2. `InventoryItem` (Tồn kho SKU) — `Fulfillment.Domain/Entities/InventoryItem.cs`
| Thuộc tính | Kiểu | Ghi chú | Bằng chứng |
|---|---|---|---|
| `SkuId` | `Guid` | Cross-service ref → product variant | `InventoryItem.cs:7` |
| `WarehouseId` | `Guid` | FK → `Warehouses.Id` (Cascade) | `InventoryItemConfiguration.cs:14-16` |
| `TotalQuantity`, `ReservedQuantity`, `UnavailableQuantity`, `AvailableQuantity` | `int` | **4 cột lưu song song**, đều `NOT NULL`, không có ràng buộc | `InventoryItem.cs:9-12`; migration `InventoryItems` |
- Unique Index: `(SkuId, WarehouseId)` — ✅ `InventoryItemConfiguration.cs:12`.
- Navigation: `Warehouse`, `ReservationItems`, `Ledgers` — ✅.
- Trạng thái: ⚠️ Có seed 5 dòng (100/200/150/10/500); **0 writer** runtime.
### 1.3. `InventoryReservation` & `InventoryReservationItem` (Giữ chỗ tồn kho)
- Quản lý khóa giữ hàng trong thời gian chờ thanh toán đơn hàng thương mại hoặc phiên đấu giá. — ❌ **Đặc tả mục tiêu**: không có use case nào tạo reservation.
| Trường | Kiểu | Ghi chú | Bằng chứng |
|---|---|---|---|
| `ReferenceType` | `InventoryReservationReferenceType` | `Order=0`, `AuctionCheckout=1`, `Manual=2`; lưu string | `InventoryReservation.cs:8`, config dòng 12 |
| `ReferenceId` | `string` | Max 100 | config dòng 13 |
| `Status` | `InventoryReservationStatus` | `Pending=0`, `Confirmed=1`, `Released=2`, `Expired=3`; lưu string | `InventoryReservation.cs:10`, config dòng 14 |
| `ExpiresAt` | `DateTimeOffset?` | **Cột có thật** nhưng **chưa có code set/đọc** — không có job hết hạn | `InventoryReservation.cs:11` |
| `ConfirmedAt`, `ReleasedAt` | `DateTimeOffset?` | Chưa có code ghi | `InventoryReservation.cs:12-13` |
- `InventoryReservationItem`: `ReservationId`, `InventoryItemId`, `Quantity` (`InventoryReservationItem.cs:7-9`).
- Trạng thái: ❌ Chưa có — **0 writer**, 0 seed, không index theo `Status`/`ExpiresAt`.
### 1.4. `InventoryLedger` (Sổ cái biến động kho) — `Fulfillment.Domain/Entities/InventoryLedger.cs`
| Trường | Kiểu | Ghi chú | Bằng chứng |
|---|---|---|---|
| `InventoryItemId` | `Guid` | FK, Cascade | `InventoryLedgerConfiguration.cs:17-19` |
| `MovementType` | `InventoryMovementType` | `Inbound=0`, `Outbound=1`, `Reserve=2`, `Release=3`, `Adjustment=4`, `Return=5`; lưu string | `InventoryLedger.cs:9`, config dòng 12 |
| `Quantity` | `int` | required | `InventoryLedger.cs:10` |
| `ReservationId` | `Guid?` | FK nullable | config dòng 21-23 |
| `ReferenceType` | `InventoryLedgerReferenceType?` | `Order=0`, `Reservation=1`, `Audit=2`, `StockIn=3`; lưu string | `InventoryLedger.cs:12`, config dòng 13 |
| `ReferenceId`, `Note` | `string?` | Max 100 / 1000 | config dòng 14-15 |
- Trạng thái: ❌ Chưa có — **0 writer**, 0 seed ⇒ không có dấu vết biến động kho nào được ghi.
### 1.5. `Carrier` (Đối tác vận chuyển) — `Fulfillment.Domain/Entities/Carrier.cs`
- `Code` (Max 100, **Unique Index** — `CarrierConfiguration.cs:12-13`), `Name` (Max 255), `Priority` (`int`), `IsDisabled`. Navigation `Shipments`.
- Trạng thái: ⚠️ Có seed 1 carrier `GHN`; **0 writer** runtime.
### 1.6. `Shipment`, `ShipmentItem`, `ShipmentEvent` (Vận đơn & Hành trình)
`Shipment` — `Fulfillment.Domain/Entities/Shipment.cs`:
| Trường | Kiểu | Ghi chú | Bằng chứng |
|---|---|---|---|
| `OrderId` | `Guid` | Cross-service ref → order/sub-order | `Shipment.cs:8` |
| `WarehouseId`, `CarrierId` | `Guid` | FK bắt buộc | `Shipment.cs:9-10` |
| `ServiceType` | `ShippingServiceType` | `Standard=0`, `Express=1`, `SameDay=2`; lưu string | config dòng 12 |
| `ShippingFee` | `decimal` | `numeric(18,2)` | `ShipmentConfiguration.cs:13` |
| `TrackingNumber` | `string?` | Max 100, **KHÔNG unique, cho phép NULL** | `ShipmentConfiguration.cs:14` |
| `Status` | `ShipmentStatus` | `ReadyToPick=0` … `Cancelled=8`; lưu string | `ShipmentConfiguration.cs:15` |
| `ReceiverName`, `ReceiverPhone`, `ReceiverAddress` | `string` | Max 255 / 50 / 1000, required | config dòng 16-18 |
| `EstimatedDeliveryAt`, `DeliveredAt` | `DateTimeOffset?` | — | `Shipment.cs:18-19` |
- Tên sai so với tài liệu cũ: `TrackingCode` **không tồn tại** — cột thật là `TrackingNumber`; `TrackingMilestones` **không tồn tại** — bảng thật là `ShipmentEvents`.
- `ShipmentItem`: `ShipmentId`, `SkuId`, `Quantity` — ✅ có seed 1 dòng.
- `ShipmentEvent`: `ShipmentId`, `CarrierStatus?`, `Status` (`ShipmentEventStatus`: `Created=0`, `PickedUp=1`, `InTransit=2`, `OutForDelivery=3`, `Delivered=4`, `DeliveryFailed=5`, `Returned=6`), `Description?`, `ExternalEventId?`, `OccurredAt`, `RawPayload?` (jsonb) — ❌ **0 writer**, 0 seed.
- Trạng thái `Shipment`: ⚠️ Có seed 1 vận đơn (`GHN-NEXUS-20260101-0001`, trạng thái `Delivered`); **0 writer** runtime ⇒ không có luồng tạo/cập nhật vận đơn.
### 1.7. `AuditLog` — `Fulfillment.Domain/Entities/AuditLog.cs`
- Ghi vết kiểm toán vận hành kho vận. ❌ **Đặc tả mục tiêu**: **0 writer** (không có interceptor/SaveChanges override nào ghi audit).
- Trường thật: `ServiceName`, `TraceId?`, `UserId?`, `UserName?`, `Action` (`AuditAction`, lưu string), `EntityName`, `EntityId`, `OldValues`/`NewValues`/`ChangedColumns` (**jsonb**), `IpAddress?`, `UserAgent?`, `CreatedAt`, `IsSynced`.
---
## 2. BẢNG DB THẬT TRONG MIGRATION
Migration duy nhất: `Fulfillment.Infrastructure/Migrations/20261024103218_InitialFulfillmentSchema.cs` — **13 bảng**.
| # | Bảng | Loại | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| 1 | `AuditLogs` | Nghiệp vụ | ⚠️ bảng có, 0 writer | migration dòng 18 |
| 2 | `Carriers` | Nghiệp vụ | ⚠️ bảng có, 0 writer (chỉ seed) | dòng 43 |
| 3 | `InventoryItems` | Nghiệp vụ | ⚠️ bảng có, 0 writer (chỉ seed) | dòng 187 |
| 4 | `InventoryLedgers` | Nghiệp vụ | ❌ bảng có, 0 writer | dòng 255 |
| 5 | `InventoryReservations` | Nghiệp vụ | ❌ bảng có, 0 writer | dòng 85 |
| 6 | `InventoryReservationItems` | Nghiệp vụ | ❌ bảng có, 0 writer | dòng 288 |
| 7 | `Shipments` | Nghiệp vụ | ⚠️ bảng có, 0 writer (chỉ seed) | dòng 214 |
| 8 | `ShipmentItems` | Nghiệp vụ | ⚠️ bảng có, 0 writer (chỉ seed) | — |
| 9 | `ShipmentEvents` | Nghiệp vụ | ❌ bảng có, 0 writer | — |
| 10 | `Warehouses` | Nghiệp vụ | ⚠️ bảng có, 0 writer (chỉ seed) | dòng 122 |
| 11 | `InboxState` | MassTransit outbox | ⚠️ hạ tầng có, chưa dùng | dòng 62 |
| 12 | `OutboxMessage` | MassTransit outbox | ⚠️ hạ tầng có, chưa dùng | dòng 145 |
| 13 | `OutboxState` | MassTransit outbox | ⚠️ hạ tầng có, chưa dùng | dòng 106 |
**Index unique thật**: `IX_Carriers_Code`, `IX_InventoryItems_SkuId_WarehouseId`, `IX_Warehouses_Code` (+ các unique index nội bộ của outbox).
### 2.1. Dữ liệu seed (chỉ có khi chạy `Migrate()`) — `Fulfillment.Infrastructure/Persistence/Seeds/FulfillmentSeedData.cs`
| Bảng | Số dòng seed |
|---|---:|
| `Warehouses` | 1 (`WH-CENTRAL-01`) |
| `Carriers` | 1 (`GHN`) |
| `InventoryItems` | 5 |
| `Shipments` | 1 (`GHN-NEXUS-20260101-0001`, `Delivered`) |
| `ShipmentItems` | 1 |
| `ShipmentEvents`, `InventoryReservations`, `InventoryReservationItems`, `InventoryLedgers`, `AuditLogs` | 0 |
---
## 3. LỖ HỔNG TOÀN VẸN DỮ LIỆU (bắt buộc biết trước khi viết code ghi)
| # | Lỗ hổng | Sự thật kiểm chứng được | Hệ quả |
|---|---|---|---|
| D1 | Không có ghi dữ liệu | **0 lời gọi `SaveChangesAsync`** ngoài khai báo interface (`IFulfillmentDbContext.cs:21`) | Mọi thay đổi trong bộ nhớ sẽ **không bao giờ** xuống DB |
| D2 | Không transaction | grep `BeginTransaction` = 0 | Không thể reserve kho + tạo shipment nguyên tử |
| D3 | Không lock | grep `FOR UPDATE`/`Serializable` = 0 | Hai request đồng thời có thể cùng đọc `AvailableQuantity` cũ ⇒ oversell |
| D4 | Không optimistic concurrency | Không có `RowVersion`/`xmin`/`IsConcurrencyToken` | Không phát hiện ghi đè đồng thời |
| D5 | Không CHECK constraint | grep `HasCheckConstraint` = 0 | DB cho phép số lượng âm, hoặc `AvailableQuantity ≠ Total - Reserved - Unavailable` |
| D6 | `AvailableQuantity` là cột lưu song song | `InventoryItem.cs:12` (không computed column, không trigger) | Không có cơ chế nào bảo đảm nó khớp 3 cột còn lại |
| D7 | `TrackingNumber` không unique | `ShipmentConfiguration.cs:14` chỉ `HasMaxLength(100)`, cột nullable | Hai vận đơn có thể trùng mã tracking; tra cứu `FirstOrDefaultAsync` trả bản ghi bất kỳ |
| D8 | `ExpiresAt` của reservation không được dùng | `InventoryReservation.cs:11` có cột, 0 code set/đọc | Reservation treo sẽ khoá tồn kho vĩnh viễn (nếu sau này có code reserve) |
| D9 | Soft delete không có query filter | Có `IsDeleted`/`DeletedAt` (`BaseEntity.cs:8-9`) nhưng grep `HasQueryFilter` = 0 | Bản ghi đã xoá mềm vẫn xuất hiện trong mọi truy vấn |
| D10 | `AppDbContext` và `FulfillmentDbContext` trùng nhau | `FulfillmentDbContext : AppDbContext` rỗng (`AppDbContext.cs:38-43`), DI chỉ đăng ký `AppDbContext` | Hai tên cho một DbContext; dễ inject nhầm, không thêm hành vi gì |
---
## 4. CẤU HÌNH EF CORE
| Hạng mục | Trạng thái | Bằng chứng |
|---|---|---|
| EF Core `8.0.10` + `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.10` | ✅ | `Fulfillment.Infrastructure/Fulfillment.Infrastructure.csproj` |
| 10 `IEntityTypeConfiguration` áp qua `ApplyConfigurationsFromAssembly` | ✅ | `AppDbContext.cs:28` |
| Enum lưu dạng string (`HasConversion<string>()`) | ✅ | các file `*Configuration.cs` |
| Transactional Outbox entity của MassTransit | ✅ | `AppDbContext.cs:31-34` |
| `Migrate()` tự động khi khởi động (nuốt lỗi bằng `LogWarning`) | ⚠️ có, nhưng không chặn app khởi động khi DB lỗi | `Fulfillment.Api/Program.cs:40-55` |
| `HasQueryFilter` cho soft delete | ❌ | grep = 0 |
| `HasCheckConstraint` | ❌ | grep = 0 |
| Concurrency token | ❌ | grep = 0 |