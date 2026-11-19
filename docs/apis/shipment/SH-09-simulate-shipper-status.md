# SH-09: Gia Lap Buu Ta Doi Trang Thai (Simulate Shipper Status)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SH-09`
- **Ten API**: Simulate Shipper Status (Sandbox Testing)
- **Phuong thuc**: `POST`
- **URL**:
  - `POST /api/v1/shipments/{trackingNumber}/simulate-shipper-status`
  - Alias: `POST /api/v1/shipments/simulate-shipper-status`
  - Docker: `http://fulfillment-service:8006/api/v1/shipments/{trackingNumber}/simulate-shipper-status`
  - Chay Local: `http://localhost:8006/api/v1/shipments/{trackingNumber}/simulate-shipper-status`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin`, `staff` (`[RequireRole("seller", "admin", "staff")]`)

## 2. Mo ta nghiep vu
Gia lap hanh dong buu ta GHN cap nhat trang thai don hang trong moi truong thu nghiem (Sandbox):
- Cap nhat `Shipments`, ghi nhan `ShipmentEvents` co thong tin buu ta va anh POD.
- Ghi nhan `WebhookAuditLog` va tu dong kich hoat `DeliveryNotificationOutbox` ban sang Commerce Service cap nhat `SubOrder` va dong bo don hang cha `Order`.

## 3. Request Body
```json
{
  "status": "Delivered",
  "description": "Giao hang thanh cong",
  "shipperName": "Nguyen Van Buu Ta",
  "shipperPhone": "0912345678",
  "podUrl": "https://cdn.ghn.vn/pod/sign_01.jpg",
  "force": false
}
```

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "trackingNumber": "GHN1029384",
  "orderId": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "previousStatus": "InTransit",
  "currentStatus": "Delivered",
  "commerceStatusNotified": "Delivered",
  "isSuccess": true,
  "message": "Shipment status simulated to 'Delivered' successfully."
}
```

## 5. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: Thieu trang thai.
- **400 Bad Request (`OUT_OF_ORDER_STATUS`)**: Trang thai di lui khong hop le.
- **404 Not Found (`SHIPMENT_NOT_FOUND`)**: Khong tim thay van don.
