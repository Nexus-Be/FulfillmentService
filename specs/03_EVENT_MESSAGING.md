# 03 — QUY CHUẨN GIAO TIẾP VÀ BÃI BỎ MESSAGE BROKER (FULFILLMENT SERVICE)

> **Trạng thái triển khai (2026-10-30):** 🛑 ĐÃ BÃI BỎ HOÀN TOÀN RABBITMQ & MASSTRANSIT — Toàn bộ cơ chế Consumer, Publisher, Outbox tables đã được loại bỏ khỏi FulfillmentService.
> **Nguồn sự thật:** code (`Fulfillment.Api/Program.cs`).
> **Cập nhật lần cuối:** 2026-10-30.

---

## 1. NGUYÊN TẮC BÃI BỎ MESSAGE BROKER

1. **Gỡ bỏ hoàn toàn MassTransit**:
   - Toàn bộ package `MassTransit` và `MassTransit.EntityFrameworkCore` đã được loại bỏ.
   - Không khai báo bất kỳ event consumer hoặc publisher nào.

2. **Giao tiếp liên dịch vụ qua REST API**:
   - Các tương tác tạo vận đơn hoặc cập nhật lộ trình giao hàng được thực hiện qua REST API đồng bộ.
