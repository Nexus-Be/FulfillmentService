# FUL-05: Lấy Lịch Sử Biến Động Sổ Cái (Get Inventory Ledger)

## 1. Thông tin tổng quan
- **Mã chức năng**: `FUL-05`
- **Tên API**: Get Inventory Ledger
- **Phương thức**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/{skuId}/ledgers`
  - Chạy Local: `http://localhost:5228/api/v1/internal/inventories/{skuId}/ledgers`
- **Phân loại**: Internal API (đối soát kế toán / audit)
- **Quyền hạn / Role**: Seller, Admin, Auditor

## 2. Mô tả nghiệp vụ
Truy vấn nhật ký kế toán kho (Stock Ledger) của một mã SKU.
- Bảng Sổ cái là **Append-only** (chỉ ghi thêm, tuyệt đối cấm sửa/xóa).
- Ghi nhận các biến động tài sản vật lý:
  - `INITIAL`: Khởi tạo kho ban đầu.
  - `INTAKE`: Nhập hàng bổ sung.
  - `COMMIT`: Xuất kho giao đơn hàng thật sự.
  - `ADJUST`: Điều chỉnh kiểm kê thực tế.

## 3. Path Parameters
| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `skuId` | `Guid` | Có | Mã SKU cần xem lịch sử sổ cái |

## 4. Phản hồi thành công (200 OK)
```json
[
  {
    "id": "11111111-2222-3333-4444-555555555555",
    "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "movementType": "INITIAL",
    "quantity": 100,
    "reservationId": null,
    "referenceType": "MANUAL",
    "referenceId": null,
    "note": "Khởi tạo tồn kho ban đầu từ kho tổng",
    "createdAt": "2026-11-01T10:00:00Z"
  },
  {
    "id": "22222222-3333-4444-5555-666666666666",
    "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "movementType": "COMMIT",
    "quantity": 2,
    "reservationId": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
    "referenceType": "ORDER",
    "referenceId": "ORD-999",
    "note": "Committed 2 for order ORD-999",
    "createdAt": "2026-11-01T10:15:00Z"
  }
]
```

## 5. Phản hồi lỗi
- **404 Not Found (`NOT_FOUND`)**: SKU không tồn tại trong kho.

