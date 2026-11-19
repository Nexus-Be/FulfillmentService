# SH-10: Tao Van Don Noi Bo (Internal Create Shipment)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SH-10`
- **Ten API**: Internal Create Shipment
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/shipments`
  - Chay Local: `http://localhost:8006/api/v1/internal/shipments`
- **Phan loai**: Internal API (Goi tu Commerce Service khi nguoi ban xac nhan don hang con)
- **Quyen han / Role**: Internal Service

## 2. Mo ta nghiep vu
Khi don hang con o trang thai `Confirmed`, Commerce Service goi API nay sang Fulfillment de tao van don thuc te tren may chu GHN:
1. **Idempotency**: Neu don hang con da co van don, he thong tra ve ngay van don cu ma khong tao van don moi tren GHN.
2. **Lay kho va nha van chuyen**: Xac dinh kho cua nguoi ban va hang GHN dang hoat dong.
3. **Goi GHN Gateway**: Gui payload tao van don `/shiip/public-api/v2/shipping-order/create` sang GHN Sandbox / Production.
4. **Luu tru**: Luu ban ghi `Shipment` voi ma `trackingNumber` tra ve tu GHN va link in nhan.

## 3. Request Body
```json
{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "subOrderId": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "sellerId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
  "recipientName": "Nguyen Van A",
  "recipientPhone": "0912345678",
  "shippingAddress": "123 Le Loi, Quan 1, TP. Ho Chi Minh",
  "toDistrictId": 1442,
  "toWardCode": "20101",
  "weightInGrams": 500,
  "codAmount": 0,
  "requiredNote": "CHOXEMHANGKHONGTHU"
}
```

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "shipmentId": "e0b1c2d3-e4f5-4a6b-8c9d-0e1f2a3b4c5d",
  "trackingNumber": "GHN1029384",
  "labelUrl": "https://fulfillment.nexus.internal/labels/e0b1c2d3-e4f5-4a6b-8c9d-0e1f2a3b4c5d.pdf",
  "status": "ReadyToPick"
}
```

## 5. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: Thieu thong tin giao hang bat buoc.
- **404 Not Found (`WAREHOUSE_NOT_FOUND`)**: Seller chua co kho hang hoat dong.
- **502 Bad Gateway (`CARRIER_ERROR`)**: GHN Gateway phan hoi loi tao don.
