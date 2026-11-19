# SH-02: Lay Chi Tiet Van Don Theo ID (Get Shipment By ID)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SH-02`
- **Ten API**: Get Shipment By ID
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/shipments/{id}`
  - Chay Local: `http://localhost:8006/api/v1/shipments/{id}`
- **Phan loai**: Public API
- **Quyen han / Role**: `buyer`, `seller`, `admin`, `staff` (`[RequireRole("buyer", "seller", "admin", "staff")]`)

## 2. Mo ta nghiep vu
Tra cuu chi tiet mot van don theo ID:
- Kiem tra quyen so huu: Buyer so huu don hang, Seller so huu kho xuat hang, hoac Admin/Staff. Neu khong khop, nem loi HTTP 403 `FORBIDDEN`.
- Tra ve danh sach kien hang (`items`) va dong thoi gian su kien van chuyen (`events`).

## 3. Path Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `id` | `Guid` (UUID) | Co | Khoa chinh van don |

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "id": "e0b1c2d3-e4f5-4a6b-8c9d-0e1f2a3b4c5d",
  "orderId": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
  "carrierId": "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f",
  "trackingNumber": "GHN1029384",
  "status": "InTransit",
  "receiverName": "Nguyen Van A",
  "receiverPhone": "0912345678",
  "receiverAddress": "123 Le Loi, Ben Nghe, Quan 1, TP.HCM",
  "buyerId": "d1e2f3a4-b5c6-4d7e-8f9a-0b1c2d3e4f5a",
  "shippingFee": 28000.0,
  "printToken": "mock_ghn_token_xyz",
  "ghnOrderCode": "GHN1029384",
  "estimatedDeliveryAt": "2026-11-21T10:00:00Z",
  "deliveredAt": null,
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "quantity": 2,
      "name": "Ao Thun Cotton",
      "price": 150000.0,
      "weight": 250,
      "length": 10,
      "width": 10,
      "height": 5
    }
  ],
  "events": [
    {
      "id": "f1a2b3c4-d5e6-4a7b-8c9d-0e1f2a3b4c5d",
      "carrierStatus": "delivering",
      "status": "InTransit",
      "description": "Shipper dang giao kien hang",
      "occurredAt": "2026-11-19T08:30:00Z",
      "shipperName": "Nguyen Van B",
      "shipperPhone": "0987654321",
      "podUrl": null,
      "warehouseName": "Kho Tong Tan Binh"
    }
  ]
}
```

## 5. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Nguoi dung khong so huu van don.
- **404 Not Found (`SHIPMENT_NOT_FOUND`)**: Khong tim thay van don.
