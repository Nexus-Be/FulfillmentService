# FUL-08: Hủy Giữ Chỗ Tồn Kho (Release Reservation)

## 1. Thông tin tổng quan
- **Mã chức năng**: `FUL-08`
- **Tên API**: Release Reservation (Cancel Hold)
- **Phương thức**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/reservations/{referenceId}/release`
  - Chạy Local: `http://localhost:5228/api/v1/internal/inventories/reservations/{referenceId}/release`
- **Phân loại**: Internal API (khách bấm Hủy đơn hoặc thanh toán thất bại)
- **Quyền hạn / Role**: Order Service, System

## 2. Mô tả nghiệp vụ
Giải phóng số lượng hàng đang bị giữ chỗ tạm thời trả về lại cho tồn kho khả dụng:
1. **Chặn đứng khi đơn đã xuất kho (Conflict Guard)**:
   - Nếu đơn hàng **ĐÃ COMMIT XUẤT KHO TRƯỚC ĐÓ** (tìm thấy trong Sổ cái hoặc phiếu đã `Confirmed`):
   - Hệ thống **chặn đứng thao tác hủy**, ném lỗi **HTTP 409 Conflict** (`RESERVATION_ALREADY_COMMITTED`) và đính kèm nguyên vẹn thông tin lần commit trước để Frontend hiển thị lý do cho người dùng.
2. **Nhả hàng về kho (Release Hold)**:
   - Nếu phiếu hợp lệ (`ACTIVE`) $\rightarrow$ `ReservedQuantity -= quantity` (hàng quay lại trạng thái bán được).
   - Xóa phiếu khỏi bảng tạm `inventory_reservations` (Cascade delete tự động dọn dẹp items).
   - **Không ghi Sổ cái** (vì lúc Hold chưa ghi thì lúc Release cũng không ghi rác vào Sổ cái).
3. **Idempotency**:
   - Nếu gọi lại nhiều lần cho một đơn đã được hủy/xóa từ trước $\rightarrow$ Trả về kết quả `RELEASED` an toàn.

## 3. Path Parameters & Query Parameters
| Tham số | Vị trí | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|---|
| `referenceId` | Path | `string` | Có | Mã đơn hàng (VD: `ORD-999`) |
| `reason` | Query | `string` | Không | Lý do hủy đơn |

## 4. Phản hồi thành công (200 OK)
```json
{
  "id": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
  "referenceId": "ORD-999",
  "status": "RELEASED",
  "expiresAt": "2026-11-01T10:30:00Z",
  "confirmedAt": null,
  "releasedAt": "2026-11-01T10:05:00Z",
  "createdAt": "2026-11-01T10:00:00Z",
  "items": []
}
```

## 5. Phản hồi lỗi: Đơn đã Commit xuất kho (409 Conflict)
Khi đơn đã xuất kho thành công mà bị gọi lệnh Hủy, hệ thống vừa báo lỗi vừa kèm toàn bộ dữ liệu đơn cũ để Frontend vẽ UI:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Reservation already committed",
  "status": 409,
  "errorCode": "RESERVATION_ALREADY_COMMITTED",
  "detail": "Cannot release reservation for reference 'ORD-999' because it was already committed at 2026-11-01T10:15:00.0000000+00:00.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
  "referenceId": "ORD-999",
  "committedAt": "2026-11-01T10:15:00.0000000+00:00",
  "reservation": {
    "id": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
    "referenceId": "ORD-999",
    "status": "CONFIRMED",
    "expiresAt": "2026-11-01T10:15:00.0000000+00:00",
    "confirmedAt": "2026-11-01T10:15:00.0000000+00:00",
    "releasedAt": null,
    "createdAt": "2026-11-01T10:15:00.0000000+00:00",
    "items": [
      {
        "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
        "quantity": 2
      }
    ]
  }
}
```

