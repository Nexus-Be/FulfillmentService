# SH-11: Huy Van Don Noi Bo Theo Don Hang (Internal Cancel Shipment)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SH-11`
- **Ten API**: Internal Cancel Shipment By Order ID
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/shipments/order/{orderId}/cancel`
  - Chay Local: `http://localhost:8006/api/v1/internal/shipments/order/{orderId}/cancel`
- **Phan loai**: Internal API (Goi tu Commerce Service khi nguoi mua hoac nguoi ban huy don hang con)
- **Quyen han / Role**: Internal Service

## 2. Mo ta nghiep vu
Huy van don giao hang dua tren ma don hang con (`orderId`):
- Neu van don o trang thai `ReadyToPick` hoac `Picking`, he thong huy van don va goi dong bo sang GHN.
- Neu don da giao thanh cong, tu choi huy va tra ve HTTP 400 `INVALID_SHIPMENT_STATUS`.

## 3. Path Parameters & Request Body
```json
// Path: /api/v1/internal/shipments/order/{orderId}/cancel
{
  "reason": "Order cancelled by buyer"
}
```

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "shipmentId": "e0b1c2d3-e4f5-4a6b-8c9d-0e1f2a3b4c5d",
  "trackingNumber": "GHN1029384",
  "isSuccess": true,
  "message": "Shipment cancelled successfully."
}
```

## 5. Phan hoi loi
- **400 Bad Request (`INVALID_SHIPMENT_STATUS`)**: Khong the huy van don o trang thai hien tai.
- **404 Not Found (`SHIPMENT_NOT_FOUND`)**: Khong tim thay van don gan voi don hang con nay.
