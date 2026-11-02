# FUL-02: Nhập Kho Hoặc Điều Chỉnh Tồn Kho (Adjust Inventory)

## 1. Thông tin tổng quan
- **Mã chức năng**: `FUL-02`
- **Tên API**: Adjust Inventory
- **Phương thức**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/inventories/adjust`
  - Chạy Local: `http://localhost:5228/api/v1/internal/inventories/adjust`
- **Phân loại**: Internal API
- **Quyền hạn / Role**: Seller, Admin

## 2. Mô tả nghiệp vụ
Dùng để:
1. **Nhập kho (`INTAKE`)**: Tăng số lượng hàng thực tế trong kho (`quantity > 0`).
2. **Kiểm kê / Điều chỉnh sai lệch (`ADJUST`)**: Tăng hoặc giảm số lượng thực tế do hàng mất mát, hỏng hóc, sai lệch số liệu (`quantity != 0`).
- Tuyệt đối không cho phép điều chỉnh khiến `TotalQuantity < ReservedQuantity` (không được làm âm hàng đã giữ của khách).
- Ghi 1 dòng Sổ cái tương ứng với `MovementType = INTAKE` hoặc `MovementType = ADJUST`.

## 3. Headers
| Header | Giá trị | Bắt buộc |
|---|---|---|
| `Content-Type` | `application/json` | Có |

## 4. Request Body
```json
{
  "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "type": 2,
  "quantity": 20,
  "reason": "Nhập thêm hàng từ nhà cung cấp đợt 2"
}
```

### Chi tiết các trường:
| Trường | Kiểu dữ liệu | Bắt buộc | Mô tả |
|---|---|---|---|
| `skuId` | `Guid` | Có | Mã SKU sản phẩm |
| `type` | `int` | Có | `2` = Intake (Nhập kho), `3` = Adjust (Điều chỉnh kiểm kê) |
| `quantity` | `int` | Có | Số lượng thay đổi (Intake phải $> 0$, Adjust có thể âm hoặc dương nhưng $\neq 0$) |
| `reason` | `string` | Có | Lý do điều chỉnh kho |

## 5. Phản hồi thành công (200 OK)
```json
{
  "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "totalQuantity": 120,
  "reservedQuantity": 0,
  "availableQuantity": 120,
  "ledgerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

## 6. Phản hồi lỗi
- **404 Not Found (`NOT_FOUND`)**: SKU chưa được khai báo trong hệ thống kho.
- **400 Bad Request (`BUSINESS_RULE_VIOLATION`)**: Điều chỉnh làm `TotalQuantity < ReservedQuantity` hoặc `TotalQuantity < 0`.

