# SH-05: Huy Van Don & Dong Bo Sang Hang Van Chuyen (Cancel Shipment)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SH-05`
- **Ten API**: Cancel Shipment
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/shipments/{id}/cancel`
  - Chay Local: `http://localhost:8006/api/v1/shipments/{id}/cancel`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin` (`[RequireRole("seller", "admin")]`)

## 2. Mo ta nghiep vu
Huy van don giao hang va gui yeu cau huy sang cong GHN:
1. **Rang buoc trang thai**: Chi duoc phep huy khi van don o trang thai `ReadyToPick` hoac `Picking`. Neu van don da ban giao cho buu ta (`HandedOver`, `InTransit`) hoac da giao (`Delivered`), he thong tu choi huy va tra ve HTTP 400 `INVALID_SHIPMENT_STATUS`.
2. **Dong bo GHN**: Goi API `/shiip/public-api/v2/switch-status/cancel` sang GHN de huy phieu giao hang.

## 3. Path Parameters & Request Body
```json
// Path: /api/v1/shipments/{id}/cancel
{
  "reason": "Khach hang yeu cau doi dia chi"
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
- **400 Bad Request (`INVALID_SHIPMENT_STATUS`)**: "Cannot cancel a shipment in '{status}' status."
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Nguoi ban khong so huu kho xuat hang.
- **404 Not Found (`SHIPMENT_NOT_FOUND`)**: Khong tim thay van don.
