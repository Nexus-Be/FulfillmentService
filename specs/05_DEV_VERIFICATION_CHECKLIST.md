# 05 — DANH SÁCH KIỂM CHỨNG & NGHIỆM THU (FULFILLMENT SERVICE CHECKLIST)
> **Trạng thái triển khai (2026-10-24):** ⚠️ Một phần — build được (`Fulfillment.sln`, 4 project), nhưng **0 project test / 0 test case** nên `dotnet test` không kiểm chứng được gì; bề mặt để nghiệm thu chỉ có `GET /health` (`Fulfillment.Api/Program.cs:57`), **0/3 rpc**, 0 consumer/publisher.
> **Nguồn sự thật:** code trong repo này (`file:line` ghi kèm trong từng bảng).
> **Cập nhật lần cuối:** 2026-10-24.
---
## 1. LỆNH BIÊN DỊCH & CHẠY THỬ NGHIỆM
```bash
# 1. Biên dịch solution (tên thật là Fulfillment.sln — KHÔNG phải FulfillmentService.sln)
dotnet build Fulfillment.sln -c Release
# 2. Khởi chạy service.
#    Lưu ý port: Docker = 8006; chạy local mặc định = 5228/7014
#    (Properties/launchSettings.json). Muốn đúng 8006 thì set URL:
ASPNETCORE_URLS=http://localhost:8006 dotnet run --project Fulfillment.Api
# 3. Nghiệm thu bề mặt DUY NHẤT hiện có:
curl http://localhost:5228/health
# => {"status":"Healthy","service":"Fulfillment.Api"}
# 4. (Không còn khả dụng) Thử tính cước qua ApiGateway:
#    gateway.runtime.json đổi "fulfillment": { "Mode": "Real", "Address": "http://localhost:8006" }
#    rồi gọi POST /gateway/api/v1/fulfillment/shipping/quote
#    ❌ KHÔNG THỂ: Fulfillment không có rpc/endpoint tính cước (0/3 rpc) và không có
#    gRPC server để Gateway gọi. Xem specs/02_API_GRPC_CONTRACTS.md.
```
> ⚠️ **Không có project test trong solution** (`Fulfillment.sln` chỉ có `Fulfillment.Api`, `Fulfillment.Application`, `Fulfillment.Domain`, `Fulfillment.Infrastructure`; 0 file chứa `[Fact]`/`[Theory]`). Vì vậy `dotnet test Fulfillment.sln -c Release` sẽ chạy 0 test — **không được** dùng nó làm bằng chứng "đã kiểm chứng nghiệp vụ".
---
## 2. CHECKLIST TRƯỚC KHI COMMIT CHO AI AGENTS
- [ ] **Biên dịch thành công 100%**: `dotnet build Fulfillment.sln -c Release`, không lỗi cảnh báo nghiêm trọng.
- [ ] **Không vi phạm rào chắn**: Không dùng Repository Pattern bọc ngoài EF Core (`IFulfillmentDbContext` chỉ là abstraction `DbContext`, không phải repository).
- [ ] **Port & Database**: Docker lắng nghe `8006`; local mặc định `5228`/`7014`; kết nối `fulfillment_db` trên port `5438` (password mặc định `MatkhaucuaTui123@` — fallback trong `Fulfillment.Infrastructure/DependencyInjection.cs:15`).
- [ ] **Git Status**: Không commit file rác (`bin/`, `obj/`, `.idea/`, `.env`).
- [ ] **Tài liệu không ảo giác**: nếu thêm rpc/event/bảng mới, cập nhật lại banner + bảng trạng thái trong `README.md` và `specs/*.md` với `file:line` thật.
- [ ] **Không tạo bề mặt giả**: `GET /health` **không** kiểm tra DB (nó trả hằng số `Healthy`), nên không được dùng `/health` làm bằng chứng "service + DB hoạt động".
---
## 3. CHECKLIST NGHIỆM THU THEO HIỆN TRẠNG (dùng để đánh giá đúng, không tự lừa)
| # | Hạng mục | Cách kiểm | Kỳ vọng hiện tại |
|---|---|---|---|
| 1 | Build | `dotnet build Fulfillment.sln -c Release` | ✅ pass (theo audit 9/9 repo build pass) |
| 2 | Test | `dotnet test Fulfillment.sln -c Release` | ❌ 0 test — không có gì để chạy |
| 3 | Endpoint | `curl :5228/health` | ✅ 200 JSON |
| 4 | gRPC | grep `MapGrpcService` | ❌ 0 |
| 5 | Consumer | grep `IConsumer` | ❌ 0 |
| 6 | Publisher | grep `IPublishEndpoint`/`.Publish(` | ❌ 0 |
| 7 | Ghi DB | grep `SaveChangesAsync` (ngoài khai báo) | ❌ 0 |
| 8 | Migration | `dotnet ef migrations list` | 1 migration `20261024103218_InitialFulfillmentSchema` |
| 9 | Outbox | bảng `OutboxMessage` | ✅ tồn tại nhưng luôn rỗng (0 message) |