# SH-12: Cap Nhat Dia Chi Nguoi Nhan Noi Bo (Internal Update Recipient)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SH-12`
- **Ten API**: Internal Update Recipient Address
- **Phuong thuc**: `PUT`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/shipments/order/{orderId}/recipient`
  - Chay Local: `http://localhost:8006/api/v1/internal/shipments/order/{orderId}/recipient`
- **Phan loai**: Internal API (Goi tu Commerce Service khi nguoi mua doi dia chi nhan hang)
- **Quyen han / Role**: Internal Service

## 2. Mo ta nghiep vu
Cap nhat ten nguoi nhan, so dien thoai, dia chi va ma quan huyen / phuong xa GHN cho kien hang truoc khi shipper lay hang:
- Chi cho phep cap nhat khi van don dang o `ReadyToPick` hoac `Picking`.
- Neu da ban giao (`HandedOver`, `InTransit`), he thong tu choi cap nhat.

## 3. Path Parameters & Request Body
```json
// Path: /api/v1/internal/shipments/order/{orderId}/recipient
{
  "recipientName": "Nguyen Van B",
  "recipientPhone": "0987654321",
  "shippingAddress": "456 Tran Hung Dao, Quan 5, TP. Ho Chi Minh",
  "toProvinceName": "TP. Ho Chi Minh",
  "toDistrictId": 1450,
  "toWardCode": "20804"
}
```

## 4. Phan hoi thanh cong (200 OK)
Tra ve `ShipmentDto` da cap nhat dia chi moi.

## 5. Phan hoi loi
- **400 Bad Request (`INVALID_SHIPMENT_STATUS`)**: Khong the doi dia chi khi kien hang da ban giao.
- **404 Not Found (`SHIPMENT_NOT_FOUND`)**: Khong tim thay van don.
