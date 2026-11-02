# WH-02: Lấy Chi Tiết Kho Hàng Theo ID (Get Warehouse By ID)

## 1. Thông tin tổng quan
- **Mã chức năng**: `WH-02`
- **Tên API**: Get Warehouse By ID
- **Phương thức**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses/{id}`
  - Chạy Local: `http://localhost:5228/api/v1/warehouses/{id}`
- **Phân loại**: Public API
- **Quyền hạn / Role**: Tất cả người dùng (Public/Anonymous)

## 2. Mô tả nghiệp vụ
Tra cứu thông tin chi tiết của một kho bãi dựa trên định danh khóa chính (`id` dạng UUID):
1. **Kiểm tra tồn tại**: Truy vấn kho bãi từ bảng `warehouses` với `AsNoTracking` để tối ưu hiệu năng đọc.
2. **Xử lý khi không tìm thấy**: Nếu không tìm thấy kho bãi với ID tương ứng, trả về mã lỗi HTTP 404 Not Found theo chuẩn RFC 7807 ProblemDetails.

## 3. Path Parameters
| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `id` | `Guid` (UUID) | Có | Mã định danh kho bãi (VD: `e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32`) |

## 4. Phản hồi thành công (200 OK)
```json
{
  "id": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
  "code": "WH-HCM-01",
  "name": "Kho Tổng Hồ Chí Minh",
  "address": "Khu Công Nghệ Cao, TP. Thủ Đức, TP. Hồ Chí Minh",
  "isActive": true,
  "sellerId": "00000000-0000-0000-0000-000000000000",
  "createdAt": "2026-10-31T08:00:00Z"
}
```

## 5. Phản hồi lỗi
- **404 Not Found (`WAREHOUSE_NOT_FOUND`)**: Không tìm thấy kho hàng với ID cung cấp.
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Warehouse not found",
  "status": 404,
  "errorCode": "WAREHOUSE_NOT_FOUND",
  "detail": "Warehouse not found with ID 'e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32'.",
  "instance": "/api/v1/warehouses/e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
}
```
- **400 Bad Request**: Tham số `id` trên URL không đúng định dạng GUID.

