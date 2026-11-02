# FUL-07: Chốt Bán Hàng & Xuất Kho Thật Sự (Commit Reservation)

## 1. Thông tin tổng quan
- **Mã chức năng**: `FUL-07`
- **Tên API**: Commit Reservation (Confirm Order Outbound)
- **Phương thức**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/reservations/{referenceId}/commit`
  - Chạy Local: `http://localhost:5228/api/v1/internal/inventories/reservations/{referenceId}/commit`
- **Phân loại**: Internal API (gọi khi có thanh toán thành công / PaymentCompleted)
- **Quyền hạn / Role**: Payment Service, Order Service, Queue Worker

## 2. Mô tả nghiệp vụ
Chính thức chốt đơn hàng và xuất kho vật lý:
1. **Xuất kho vật lý**:
   - `TotalQuantity -= Quantity` (Hàng thật sự biến mất khỏi kho).
   - `ReservedQuantity -= Quantity` (Giải phóng số lượng tạm giữ).
2. **Ghi Sổ cái kế toán**:
   - Ghi 1 dòng vào `inventory_ledger` với `MovementType = COMMIT` cho mỗi mặt hàng.
3. **Giải phóng bảng tạm (Decoupled Hard-Delete)**:
   - Xóa phiếu khỏi bảng `inventory_reservations` để database không bị phình to (Cascade delete tự dọn dẹp các dòng items).
4. **Bảo vệ Idempotency khi gọi lại (Retry Protection)**:
   - Nếu mạng chập chờn gửi lại request lần 2: Bảng tạm đã bị xóa, hệ thống sẽ tra cứu Sổ cái `inventory_ledger`.
   - Nếu thấy đã có bản ghi `COMMIT` $\rightarrow$ Tái hiện lại đúng 100% dữ liệu của **lần trừ kho trước đó** (thời điểm commit thật và danh sách items) để trả về thành công, **không trừ kho lần 2**.

## 3. Path Parameters
| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `referenceId` | `string` | Có | Mã đơn hàng (VD: `ORD-999`) |

## 4. Phản hồi thành công (200 OK)
```json
{
  "id": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
  "referenceId": "ORD-999",
  "status": "CONFIRMED",
  "expiresAt": "2026-11-01T10:15:00Z",
  "confirmedAt": "2026-11-01T10:15:00Z",
  "releasedAt": null,
  "createdAt": "2026-11-01T10:15:00Z",
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "quantity": 2
    }
  ]
}
```

## 5. Phản hồi lỗi
- **404 Not Found (`NOT_FOUND`)**: Mã `referenceId` không tồn tại ở cả kho tạm lẫn Sổ cái (dữ liệu rác hoặc giả mạo).
- **400 Bad Request (`BUSINESS_RULE_VIOLATION`)**: Phiếu giữ chỗ ở trạng thái không hợp lệ (ví dụ đã bị hết hạn hoặc đã bị hủy trước đó).

