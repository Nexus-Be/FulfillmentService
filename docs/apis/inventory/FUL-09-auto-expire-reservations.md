# FUL-09: Quét & Hủy Phiếu Giữ Chỗ Quá Hạn (Auto Expire Reservations)

## 1. Thông tin tổng quan
- **Mã chức năng**: `FUL-09` (Tương ứng FUL-11 trong CSV)
- **Tên API**: Auto Expire Reservations (Background Job Endpoint)
- **Phương thức**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/reservations/auto-expire`
  - Chạy Local: `http://localhost:5228/api/v1/internal/inventories/reservations/auto-expire`
- **Phân loại**: Internal API (Cronjob / Background Worker / Kubernetes Cron gọi định kỳ mỗi phút)
- **Quyền hạn / Role**: System, Cronjob

## 2. Mô tả nghiệp vụ
Tự động dọn dẹp các đơn hàng khách bấm giữ chỗ nhưng bỏ quên, không thanh toán sau 30 phút (`holdMinutes`):
1. **Tìm kiếm**: Quét tất cả các phiếu `inventory_reservations` đang có `Status == 'ACTIVE'` và `ExpiresAt <= NOW()`.
2. **Hoàn hàng về kho khả dụng**:
   - Gom tất cả các mặt hàng của các phiếu này lại.
   - Gọi hàm dùng chung `ReleaseHoldItemsAsync` để giảm `ReservedQuantity` tương ứng $\rightarrow$ hàng ngay lập tức quay lại trạng thái sẵn sàng bán cho khách khác.
3. **Dọn sạch rác Database**:
   - Xóa toàn bộ các phiếu hết hạn này khỏi bảng `inventory_reservations`.
   - Trả về số lượng phiếu đã được giải phóng thành công.

## 3. Headers
| Header | Giá trị | Bắt buộc |
|---|---|---|
| `Content-Type` | `application/json` | Có |

## 4. Phản hồi thành công (200 OK)
```json
{
  "expiredCount": 5
}
```
*(Trong đó `expiredCount` là số lượng phiếu giữ chỗ quá hạn đã được dọn dẹp và hoàn hàng về kho)*.

