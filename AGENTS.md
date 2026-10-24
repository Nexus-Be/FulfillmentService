<!-- gitnexus:start -->
# GitNexus — Code Intelligence
This project is indexed by GitNexus as **FulfillmentService** (404 symbols, 514 relationships, 4 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.
> Index stale? Run `node .gitnexus/run.cjs analyze` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? `npx gitnexus analyze` (npm 11 crash → `npm i -g gitnexus`; #1939).
## Always Do
- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows. For regression review, compare against the default branch: `detect_changes({scope: "compare", base_ref: "main"})`.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `query({search_query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `context({name: "symbolName"})`.
- For security review, `explain({target: "fileOrSymbol"})` lists taint findings (source→sink flows; needs `analyze --pdg`).
## Never Do
- NEVER edit a function, class, or method without first running `impact` on it.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis.
- NEVER rename symbols with find-and-replace — use `rename` which understands the call graph.
- NEVER commit changes without running `detect_changes()` to check affected scope.
## Resources
| Resource | Use for |
|----------|---------|
| `gitnexus://repo/FulfillmentService/context` | Codebase overview, check index freshness |
| `gitnexus://repo/FulfillmentService/clusters` | All functional areas |
| `gitnexus://repo/FulfillmentService/processes` | All execution flows |
| `gitnexus://repo/FulfillmentService/process/{name}` | Step-by-step execution trace |
## CLI
| Task | Read this skill file |
|------|---------------------|
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus/gitnexus-cli/SKILL.md` |
<!-- gitnexus:end -->
---
# QUY CHUẨN VẬN HÀNH DÀNH CHO AI CODING AGENTS (FULFILLMENT SERVICE)
> **Trạng thái triển khai (2026-10-24):** ❌ Chưa có bề mặt — **0/3 rpc** gRPC, **0 controller**, **0 consumer**, **0/3 event** publish, **0 test**; duy nhất `GET /health` (`Fulfillment.Api/Program.cs:57`). Toàn bộ service **read-only** (0 lời gọi `SaveChangesAsync`).
> **Nguồn sự thật:** code trong repo này (`file:line` ghi kèm trong từng bảng).
> **Cập nhật lần cuối:** 2026-10-24.
---
## 🛑 NGUYÊN TẮC 1: BẮT BUỘC LẬP KẾ HOẠCH TRƯỚC KHI HÀNH ĐỘNG (PLAN-FIRST RULE)
Mọi AI Agent khi nhận nhiệm vụ kỹ thuật tại FulfillmentService **BẮT BUỘC PHẢI LẬP VÀ TRÌNH BÀY PLAN** trước khi thực hiện bất kỳ hành động sửa đổi mã nguồn, thêm/xóa file, refactor, tạo migration database hay thay đổi cấu hình nào.
Một Plan chuẩn bắt buộc phải có:
1. **Mục tiêu (Objective)**: Tóm tắt chính xác điều cần làm.
2. **Context & Files cần đọc**: Liệt kê các file nội bộ của FulfillmentService cần đọc theo đúng ma trận đọc file tại Nguyên tắc 3.
3. **Phân tích rủi ro & Phạm vi ảnh hưởng (Impact Analysis)**:
   - Dùng GitNexus `impact` đánh giá blast radius các symbol bị ảnh hưởng.
   - Cảnh báo rõ nếu mức độ rủi ro là HIGH hoặc CRITICAL.
4. **Checklist các bước thực hiện (Step-by-Step Implementation)**: Từng bước logic, chi tiết.
5. **Kế hoạch kiểm chứng (Verification)**: `dotnet build Fulfillment.sln -c Release` và `dotnet test Fulfillment.sln -c Release` (⚠️ hiện 0 project test — xem Nguyên tắc 7).
---
## 🧭 NGUYÊN TẮC 2: QUY TẮC PHẠM VI NỘI BỘ (INWARD-ONLY / SELF-CONTAINED)
FulfillmentService là một Git Repository độc lập (`FulfillmentService.git`):
- **TUYỆT ĐỐI CHỈ ĐƯỢC trỏ tới các tài nguyên nội bộ của FulfillmentService**.
- **TUYỆT ĐỐI KHÔNG ĐƯỢC trỏ ngược ra ngoài Root** (`../../../docs/...`) hay sang các Service khác (`../../Catalog/...`).
- Toàn bộ tài liệu, hướng dẫn và cấu hình trong repo này phải 100% tự cung tự cấp (Self-contained).
---
## 📚 NGUYÊN TẮC 3: DANH MỤC FILE NỘI BỘ BẮT BUỘC PHẢI ĐỌC TẠI FULFILLMENT SERVICE
Khi bắt đầu làm việc tại FulfillmentService, AI Agent **BẮT BUỘC PHẢI ĐỌC** các file nội bộ sau theo thứ tự:
1. `specs/00_AGENT_MISSION_GUARDRAILS.md`: Bounded context kho bãi vật lý, vận đơn, cước phí; port `8006` (Docker) / `5228` (local), DB `5438` (`fulfillment_db`); biên giới `AvailableQuantity` (Fulfillment) vs `Available` (Catalog).
2. `specs/01_CLEAN_ARCHITECTURE_LAYERS.md`: Cấu trúc 4 tầng Clean Architecture, No-Repository pattern over EF Core.
3. `specs/02_API_GRPC_CONTRACTS.md`: Hợp đồng gRPC Server (**đặc tả mục tiêu**: `CalculateShippingFee`, `GetInventory`, `GetShipmentTracking` — hiện **0/3 rpc** đã cài đặt).
4. `specs/03_EVENT_MESSAGING.md`: Sự kiện RabbitMQ (**đặc tả mục tiêu**: consume `IPaymentCompletedEvent`; publish `ShipmentCreatedEvent`, `ShipmentStatusUpdatedEvent`, `ShipmentDeliveredEvent` — hiện **0 consumer, 0 publisher**).
5. `specs/04_DATA_PERSISTENCE.md`: Schema PostgreSQL thật (10 bảng nghiệp vụ + 3 bảng outbox): `Warehouses`, `InventoryItems`, `InventoryReservations`, `InventoryReservationItems`, `InventoryLedgers`, `Carriers`, `Shipments`, `ShipmentItems`, `ShipmentEvents`, `AuditLogs`.
6. `specs/05_DEV_VERIFICATION_CHECKLIST.md`: Lệnh build & test kiểm chứng service (⚠️ 0 test).
7. `README.md`: Cấu trúc thư mục thật của 4 project + port thật (Docker `8006`, local `5228`/`7014`).
---
## 🔒 NGUYÊN TẮC 4: RÀO CHẮN BẤT BIẾN TẠI FULFILLMENT SERVICE
1. **TUYỆT ĐỐI KHÔNG DÙNG REPOSITORY PATTERN BỌC NGOÀI EF CORE**: Sử dụng trực tiếp `AppDbContext`/`FulfillmentDbContext` và `DbSet<T>` (hoặc interface `IFulfillmentDbContext`) trong tầng Application. Cấm tạo `IShipmentRepository`, `IWarehouseRepository`, `UnitOfWork`.
2. **RANH GIỚI VẬN HÀNH KHO VẬT LÝ**: Quản lý kích thước kiện hàng, trọng lượng, vị trí kho bãi, mã tracking. Tuyệt đối không can thiệp vào tồn kho ảo sẵn sàng bán của `CatalogService`. Giữ nguyên quyết định: **Fulfillment sở hữu kho vật lý, Catalog sở hữu product/category**; hai cột `AvailableQuantity` (Fulfillment) và `Available` (Catalog) **chưa có quy tắc đồng bộ nào** — không được viết tài liệu như thể đã đồng bộ.
3. **CẤU HÌNH PORT 8006 VÀ DATABASE 5438 LÀ BẤT BIẾN** — nhưng phải ghi đủ: Docker `8006` (`Dockerfile:3-4`), chạy local `http=5228` / `https=7014` (`Fulfillment.Api/Properties/launchSettings.json`). DB `5438`, database `fulfillment_db`.
4. **HỢP ĐỒNG PROTOBUF ĐỒNG BỘ 100% VỚI GATEWAY** — ⚠️ hiện repo **không có** `Contracts/shop.proto` (thư mục `Contracts/` không tồn tại) và chưa có gRPC server. Không được mô tả là đã có.
5. **KHÔNG ĐƯỢC TẠO BỀ MẶT GIẢ**: `GET /health` trả hằng số `Healthy`, **không kiểm tra DB** (`Fulfillment.Api/Program.cs:57`). Không dùng `/health` làm bằng chứng "service + DB hoạt động".
---
## 💬 NGUYÊN TẮC 5: QUY CHUẨN THÔNG BÁO LỖI TOÀN HỆ THỐNG (MANDATORY ERROR RULES)
Tất cả các thông báo lỗi (Error Messages, Exceptions, ProblemDetails, Validation Errors, Response Details) **BẮT BUỘC** phải tuân theo 3 nguyên tắc sau:
1. **Ngôn ngữ (100% English)**: Toàn bộ thông báo lỗi phải được viết bằng tiếng Anh. Tuyệt đối không dùng tiếng Việt trong chuỗi thông báo lỗi trả về cho client.
2. **Quy cách viết hoa (Sentence case)**: Viết hoa duy nhất chữ cái đầu câu, các từ tiếp theo viết thường (ngoại trừ tên riêng, từ viết tắt hoặc định danh kỹ thuật như `ID`, `OTP`, `JWT`, `URL`, `UUID`, `Email`...).
   - ✅ Đúng: `"Shipment not found with the provided tracking number."`
   - ✅ Đúng: `"Warehouse location is inactive or invalid."`
   - ❌ Sai: `"Không tìm thấy vận đơn"` (Không dùng tiếng Việt)
   - ❌ Sai: `"shipment not found"` (Chữ cái đầu không viết hoa)
   - ❌ Sai: `"SHIPMENT NOT FOUND"` (Cấm viết hoa toàn bộ)
   - ❌ Sai: `"Shipment Not Found Provided"` (Không dùng Title Case)
3. **Mã lỗi (Error Codes — SCREAMING_SNAKE_CASE)**: Mã lỗi luôn viết hoa toàn bộ và nối bằng dấu gạch dưới:
   - Ví dụ: `SHIPMENT_NOT_FOUND`, `WAREHOUSE_INACTIVE`, `VAL_INVALID_INPUT`, `UNAUTHORIZED`, `FORBIDDEN`.
   - ✅ Mã đang dùng thật trong `Fulfillment.Api/Middleware/GlobalExceptionHandlerMiddleware.cs:36-63`: `NOT_FOUND`, `BUSINESS_RULE_VIOLATION`, `DOMAIN_ERROR`, `VAL_INVALID_ARGUMENT`, `INTERNAL_SERVER_ERROR`.
   - ❌ `SHIPMENT_NOT_FOUND`, `WAREHOUSE_INACTIVE` chưa được dùng ở đâu (đặc tả mục tiêu).
---
## 🎯 NGUYÊN TẮC 6: ĐIỀU KIỆN HOÀN THÀNH NHIỆM VỤ (DEFINITION OF DONE)
Một task tại FulfillmentService chỉ hoàn tất khi:
- [ ] Đã lập Plan chi tiết trước khi thực hiện.
- [ ] Không chứa bất kỳ liên kết trỏ ngược ra ngoài FulfillmentService.
- [ ] `dotnet build Fulfillment.sln -c Release` thành công 100% (0 error). **Tên solution thật là `Fulfillment.sln`** (không phải `FulfillmentService.sln`).
- [ ] `dotnet test Fulfillment.sln -c Release` — ⚠️ hiện solution **không có project test** (0 case), nên bước này không thể là bằng chứng nghiệm thu nghiệp vụ. Nếu task động vào nghiệp vụ kho/vận đơn, phải tạo test trước khi tuyên bố "xong".
- [ ] Trạng thái Git sạch, không commit file rác (`bin/`, `obj/`, `.vs/`, `.idea/`, `.env`).
---
## 🧪 NGUYÊN TẮC 7: HIỆN TRẠNG FULFILLMENT SERVICE (BẮT BUỘC ĐỌC TRƯỚC KHI CODE)
> Số liệu đo trực tiếp trên repo này ngày 2026-10-24: **46 file `.cs` / 1.184 LOC** (không tính `Migrations/`) — Infrastructure 13/492 · Domain 23/334 · Application 8/204 · Api 2/154. **0 test.**
| # | Sự thật | Bằng chứng |
|---|---|---|
| 1 | Bề mặt duy nhất là `GET /health`; **0 gRPC, 0 controller** | `Fulfillment.Api/Program.cs:57`; grep `MapGrpcService`/`AddControllers` = 0 |
| 2 | **0/3 rpc** trong `specs/02` đã cài đặt | grep `AddGrpc` = 0; không có `Contracts/shop.proto` |
| 3 | **0 consumer, 0 publisher** — MassTransit 8.3.0 + RabbitMQ + EF Outbox đã đăng ký nhưng không có message flow | `Fulfillment.Infrastructure/DependencyInjection.cs:23-48`; grep `IConsumer`/`IPublishEndpoint` = 0 |
| 4 | Toàn service **read-only**: `ShipmentService`/`WarehouseService` chỉ `AsNoTracking`, **0 lời gọi `SaveChangesAsync`** | `Fulfillment.Application/Services/**`; `IFulfillmentDbContext.cs:21` chỉ khai báo |
| 5 | `CreateShipmentDto` **không được dùng ở đâu** | `Fulfillment.Application/Services/Shipment/ShipmentDtos.cs:16-22` |
| 6 | **5 entity 0 writer**: `InventoryLedger`, `InventoryReservation`, `InventoryReservationItem`, `ShipmentEvent`, `AuditLog` | 0 lời gọi `Add`/`Update`/`SaveChanges` cho các tập này |
| 7 | Block map 10 dòng trong `ShipmentService.cs` **trùng 100%** ở 2 method | `ShipmentService.cs:26-35` và `46-55` |
| 8 | `AppDbContext` và `FulfillmentDbContext` **trùng nhau** (`FulfillmentDbContext` là lớp con rỗng) | `Fulfillment.Infrastructure/Persistence/AppDbContext.cs:38-43` |
| 9 | **0 transaction, 0 lock, 0 optimistic concurrency, 0 CHECK constraint**; `InventoryItem.AvailableQuantity` là cột lưu song song | grep `BeginTransaction`/`FOR UPDATE`/`RowVersion`/`HasCheckConstraint` = 0; `InventoryItem.cs:12` |
| 10 | `Shipment.TrackingNumber` **không unique**, cho phép NULL | `ShipmentConfiguration.cs:14` |
| 11 | Soft delete có cột nhưng **không có query filter** | `BaseEntity.cs:8-9`; grep `HasQueryFilter` = 0 |
| 12 | Port local lệch đặc tả: `5228`/`7014` vs `8006` | `Fulfillment.Api/Properties/launchSettings.json` vs `Dockerfile:3-4` |
| 13 | MassTransit thật là **8.3.0** (tài liệu cũ ghi 8.4.1 — sai) | `Fulfillment.Infrastructure/Fulfillment.Infrastructure.csproj:22-24` |
**Quy tắc rút ra:**
- Mọi tài liệu khi sửa **phải có banner trạng thái** (✅ CÓ / ⚠️ MỘT PHẦN / ❌ CHƯA CÓ + nguồn sự thật + ngày) và bảng có cột `Trạng thái` + `Bằng chứng` (`file:line`).
- Chỗ nào chưa có trong code phải ghi rõ "**đặc tả mục tiêu, chưa triển khai**"; **cấm** dùng từ khẳng định như "service cung cấp", "hệ thống hỗ trợ" cho thứ chưa tồn tại.
- **Code nghiệp vụ mới** (reserve kho, tạo shipment, consumer thanh toán) chỉ được bắt đầu khi: (a) đã chốt hợp đồng proto dùng chung, (b) đã có test cho double-reserve/oversell, (c) đã đọc mục FU-01…FU-07.
- Thứ tự ưu tiên triển khai theo audit: `GetInventory` + `CalculateShippingFee` (bề mặt) → consumer `IPaymentCompletedEvent` → publisher `IShipmentCreatedEvent` → reservation transaction/lock.