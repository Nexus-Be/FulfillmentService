# FUL-06: Giữ Chỗ Tồn Kho Tạm Thời (Create Reservation)

## 1. Thông tin tổng quan
- **Mã chức năng**: `FUL-06`
- **Tên API**: Create Reservation (Hold Inventory)
- **Phương thức**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/reservations`
  - Chạy Local: `http://localhost:5228/api/v1/internal/inventories/reservations`
- **Phân loại**: Internal API (OrderService / Checkout gọi khi khách bấm Đặt hàng)
- **Quyền hạn / Role**: Order Service, System

## 2. Mô tả nghiệp vụ
Giữ chỗ tạm thời số lượng hàng hóa trong 30 phút để chờ người mua thanh toán:
1. **Batch Validation (Quét toàn bộ danh sách)**:
   - Quét qua tất cả SKU trong request.
   - Bắt các lỗi: Mã SKU không tồn tại trong kho (`SKU_NOT_FOUND` do can thiệp gói tin/F12) và Tồn kho khả dụng không đủ (`INSUFFICIENT_STOCK`).
   - Nếu có lỗi, gom tất cả vào mảng `items` và ném lỗi 400 Bad Request ngay, **không giữ dở dang bất kỳ món nào (All-or-Nothing)**.
2. **Idempotency (Chống giữ trùng)**:
   - Nếu `referenceId` đã tồn tại phiếu đang `ACTIVE` $\rightarrow$ Trả về thông tin phiếu cũ, không cộng dồn giữ thêm.
3. **Không ghi Sổ cái**:
   - Chỉ tăng `ReservedQuantity`, **tuyệt đối không ghi bản ghi nào vào Sổ cái `inventory_ledger`** để tránh rác database.

## 3. Headers
| Header | Giá trị | Bắt buộc |
|---|---|---|
| `Content-Type` | `application/json` | Có |

## 4. Request Body
```json
{
  "referenceId": "ORD-999",
  "holdMinutes": 30,
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "quantity": 2
    },
    {
      "skuId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 1
    }
  ]
}
```

## 5. Phản hồi thành công (200 OK)
```json
{
  "id": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
  "referenceId": "ORD-999",
  "status": "ACTIVE",
  "expiresAt": "2026-11-01T10:30:00Z",
  "confirmedAt": null,
  "releasedAt": null,
  "createdAt": "2026-11-01T10:00:00Z",
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "quantity": 2
    },
    {
      "skuId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 1
    }
  ]
}
```

## 6. Phản hồi lỗi (Batch Error RFC 7807)
Khi có 1 hoặc nhiều mặt hàng không hợp lệ, hệ thống trả về **HTTP 400 Bad Request** kèm danh sách `items` chi tiết để Frontend tô đỏ từng dòng:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Insufficient inventory",
  "status": 400,
  "errorCode": "INSUFFICIENT_INVENTORY",
  "detail": "One or more items in the reservation request are invalid, unavailable, or have insufficient stock.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "errorCode": "SKU_NOT_FOUND",
      "requestedQuantity": 2,
      "availableQuantity": 0,
      "message": "Inventory item for SKU '9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d' was not found in warehouse inventory."
    },
    {
      "skuId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "errorCode": "INSUFFICIENT_STOCK",
      "requestedQuantity": 10,
      "availableQuantity": 3,
      "message": "Requested quantity (10) exceeds available stock (3) for SKU '3fa85f64-5717-4562-b3fc-2c963f66afa6'."
    }
  ]
}
```

