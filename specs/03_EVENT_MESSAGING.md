# 03 — SỰ KIỆN BẤT ĐỒNG BỘ QUA RABBITMQ (FULFILLMENT SERVICE EVENT MESSAGING)

FulfillmentService tham gia vào giai đoạn sau thanh toán (post-checkout fulfillment) khi đơn hàng đã được trả tiền thành công.

---

## 1. SỰ KIỆN LẮNG NGHE (CONSUMERS)

1. **`OrderPaidEvent`** (Do `CommerceService` phát hành):
   - **Xử lý**: Tự động tạo bản ghi `Shipment` (Vận đơn), cấp mã `TrackingCode` ngẫu nhiên chuẩn (ví dụ: `"VNPOST-8899213"`), chuyển trạng thái vận đơn sang `Preparing`.
   - **Phát hành tiếp**: Bắn sự kiện `ShipmentCreatedEvent`.

---

## 2. SỰ KIỆN PHÁT HÀNH (EVENTS PUBLISHED)

| Tên Sự Kiện | Thời Điểm Kích Hoạt | Payload Dữ Liệu | Services Quan Tâm |
|:---|:---|:---|:---|
| `ShipmentCreatedEvent` | Phiếu xuất kho & mã vận đơn được tạo | `OrderId`, `TrackingCode`, `Carrier`, `EstimatedDelivery` | `CommerceService` (Cập nhật Order status thành `Preparing`), `NotificationService` (Báo mã tracking cho khách). |
| `ShipmentStatusUpdatedEvent`| Hành trình kiện hàng thay đổi vị trí/trạng thái | `TrackingCode`, `Status`, `CurrentLocation`, `UpdatedAt` | `NotificationService`, `AuditService`. |
| `ShipmentDeliveredEvent` | Khách nhận hàng thành công | `OrderId`, `TrackingCode`, `DeliveredAt` | `CommerceService` (Hoàn tất đơn hàng `Completed`), `WalletService` (Giải phóng tiền cho Seller). |
