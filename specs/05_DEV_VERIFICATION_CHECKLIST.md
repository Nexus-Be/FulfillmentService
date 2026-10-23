# 05 — DANH SÁCH KIỂM CHỨNG & NGHIỆM THU (FULFILLMENT SERVICE CHECKLIST)

---

## 1. LỆNH BIÊN DỊCH & CHẠY THỬ NGHIỆM

```bash
# 1. Biên dịch solution Fulfillment
dotnet build Fulfillment.sln -c Release

# 2. Khởi chạy service (gRPC port 8006)
dotnet run --project Fulfillment.Api

# 3. Thử nghiệm qua ApiGateway:
# Mở gateway.runtime.json, đổi "fulfillment": { "Mode": "Real", "Address": "http://localhost:8006" }
# Gọi POST /gateway/api/v1/fulfillment/shipping/quote trên Swagger Gateway xem kết quả thật trả về.
```

---

## 2. CHECKLIST TRƯỚC KHI COMMIT CHO AI AGENTS

- [ ] **Biên dịch thành công 100%**: Không lỗi cảnh báo nghiêm trọng.
- [ ] **Không vi phạm rào chắn**: Không dùng Repository Pattern bọc ngoài EF Core.
- [ ] **Port & Database**: Lắng nghe đúng port `8006`, kết nối `fulfillment_db` trên port `5438` (Password: `MatkhaucuaTui123@`).
- [ ] **Git Status**: Không commit file rác (`bin/`, `obj/`, `.idea/`, `.env`).
