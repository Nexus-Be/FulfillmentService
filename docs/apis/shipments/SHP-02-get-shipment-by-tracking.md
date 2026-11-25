# FULFILLMENT - GET /api/v1/shipments/tracking/{trackingNumber}

> **Feature / Use Case:** Tra cứu thông tin vận đơn theo mã vận đơn GHN (Track Shipment)
> **Trạng thái:** ✅ VERIFIED
> **Ngày tạo:** 2026-11-25
> **Tác giả:** Antigravity AI Agent

---

## 1. Thông tin Định tuyến & RBAC (Routing & Authorization)
- **HTTP Method:** `GET`
- **Route Endpoint:** `/api/v1/shipments/tracking/{trackingNumber}`
- **Quyền hạn (RBAC Policy):** Đăng nhập (`User`, `Buyer`, `Seller`, `StaffOrAdmin`)
- **Quy tắc Sở hữu (Resource Ownership):** Buyer sở hữu đơn, Seller gửi hàng, hoặc Staff/Admin mới được xem chi tiết.
- **Controller:** `Fulfillment.Api/Controllers/Public/ShipmentsController.cs`
- **Application Contract:** `Fulfillment.Application/Services/Shipment/IShipmentService.cs`

---

## 2. Đầu vào & Quy chuẩn Validation (Input & Validation Schema)

### 2.1 Route Parameters
| Tham số | Vị trí | Kiểu C# | Bắt buộc | Ràng buộc / Giới hạn | Ghi chú |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `trackingNumber` | Route | `string` | Có | Tối đa 50 ký tự | Mã vận đơn GHN (ví dụ: `LY9KPR`) |

---

## 3. Đầu ra & Hợp đồng DTO thuần (Response Contract - Pure DTO)
- **Tên C# Record:** `ShipmentDto`
- **Mã HTTP thành công:** `200 OK`
- **JSON Response Body mẫu (camelCase):**
```json
{
  "id": "e4f5a6b7-c8d9-40e1-b2c3-d4e5f6a7b8c9",
  "orderId": "c2f9d801-44bf-4dc7-84ec-86f7b9492651",
  "trackingNumber": "LY9KPR",
  "status": "InTransit",
  "shippingFee": 20900.00,
  "receiverName": "Quốc Hoàng",
  "receiverPhone": "0987654321",
  "receiverAddress": "123 Hai Bà Trưng, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
  "toDistrictId": 1442,
  "toWardCode": "20101",
  "weight": 500,
  "length": 20,
  "width": 15,
  "height": 10,
  "ghnOrderCode": "LY9KPR",
  "carrierName": "GHN",
  "estimatedDeliveryAt": "2026-11-26T16:59:59Z",
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "quantity": 1,
      "name": "Nexus Test Product"
    }
  ],
  "events": [
    {
      "carrierStatus": "delivering",
      "status": "InTransit",
      "description": "Bưu tá đang giao hàng",
      "occurredAt": "2026-11-25T13:30:00Z",
      "shipperName": "Bưu tá GHN",
      "shipperPhone": "0988888888"
    }
  ]
}
```

---

## 4. Ma trận Mã lỗi & Ngoại lệ (RFC 9110 Error Matrix)
| HTTP Status | Mã lỗi (`SCREAMING_SNAKE_CASE`) | Thông báo lỗi chuẩn (`Sentence case`) | Điều kiện kích hoạt |
| :---: | :--- | :--- | :--- |
| `400` | `VAL_INVALID_INPUT` | "Tracking number cannot be empty." | Mã vận đơn rỗng hoặc chỉ có khoảng trắng. |
| `403` | `FORBIDDEN` | "You do not have permission to view this shipment." | Người dùng không phải là người mua sở hữu đơn, người bán, hoặc staff/admin. |
| `404` | `SHIPMENT_NOT_FOUND` | "Shipment with tracking number '{trackingNumber}' was not found." | Không tìm thấy mã vận đơn trong hệ thống. |

---

## 5. Danh mục Kiểm tra Hoàn tất (Completion Checklist)
- [x] Application Layer: `IShipmentService.GetByTrackingNumberAsync`.
- [x] API Layer: `ShipmentsController.GetByTrackingNumber` (`GET /api/v1/shipments/tracking/{trackingNumber}`).
- [x] Automated Testing: Unit tests & API tests đạt 100% PASS (`ShipmentSecurityAndLocationTests.cs`).
- [x] Status: **COMPLETED**
