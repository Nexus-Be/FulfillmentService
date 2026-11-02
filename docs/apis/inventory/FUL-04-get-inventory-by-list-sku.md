# FUL-04: Lấy Tồn Kho Danh Sách SKU Hàng Loạt (Get Inventory By List SKU)

## 1. Thông tin tổng quan
- **Mã chức năng**: `FUL-04`
- **Tên API**: Get Inventory By List SKU (Batch Get)
- **Phương thức**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/batch-get`
  - Chạy Local: `http://localhost:5228/api/v1/internal/inventories/batch-get`
- **Phân loại**: Internal API (dùng cho Cart / Checkout để render giỏ hàng)
- **Quyền hạn / Role**: Buyer, Seller, Admin, Staff

## 2. Mô tả nghiệp vụ
Cho phép truyền một mảng danh sách các mã SKU để lấy thông tin tồn kho cùng 1 lúc (Batch Query), tránh việc client phải gửi N requests làm nghẽn mạng (N+1 Problem).

## 3. Headers
| Header | Giá trị | Bắt buộc |
|---|---|---|
| `Content-Type` | `application/json` | Có |

## 4. Request Body
```json
[
  "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "3fa85f64-5717-4562-b3fc-2c963f66afa6"
]
```

## 5. Phản hồi thành công (200 OK)
Trả về danh sách các SKU tìm thấy trong kho:
```json
[
  {
    "id": "7ca92c12-3214-4112-a123-1c963f66bf11",
    "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "warehouseId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
    "totalQuantity": 100,
    "reservedQuantity": 15,
    "availableQuantity": 85
  },
  {
    "id": "8ca92c12-3214-4112-a123-1c963f66bf22",
    "skuId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "warehouseId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
    "totalQuantity": 50,
    "reservedQuantity": 0,
    "availableQuantity": 50
  }
]
```
*(Nếu một SKU trong mảng không có hàng trong kho, hệ thống sẽ bỏ qua SKU đó thay vì ném lỗi, trả về danh sách những SKU tìm thấy)*.

