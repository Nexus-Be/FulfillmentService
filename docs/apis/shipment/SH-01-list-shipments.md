# SH-01: Danh Sach Van Don Phan Trang & Bo Loc (List Shipments)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SH-01`
- **Ten API**: List Shipments
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/shipments`
  - Chay Local: `http://localhost:8006/api/v1/shipments`
- **Phan loai**: Public API
- **Quyen han / Role**: `buyer`, `seller`, `admin`, `staff` (`[RequireRole("buyer", "seller", "admin", "staff")]`)

## 2. Mo ta nghiep vu
Truy van danh sach van don giao hang co phan trang va cach ly du lieu nghiem ngat:
- **Buyer**: Tu dong loc cac van don do nguoi dung dat mua (`buyerId == currentUserId`).
- **Seller**: Tu dong loc cac van don xuat tu cac kho thuoc quyen so huu cua Seller.
- **Admin / Staff**: Co quyen xem toan bo van don cua tat ca cac kho tren he thong.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Authorization` | `Bearer <JWT_TOKEN>` | Co |

## 4. Query Parameters
| Tham so | Kieu | Mac dinh | Mo ta |
|---|---|---|---|
| `warehouseId` | `Guid?` | `null` | Loc theo kho hang |
| `orderId` | `Guid?` | `null` | Loc theo don hang con |
| `trackingNumber` | `string?` | `null` | Tim theo ma van don GHN (khong phan biet hoa thuong) |
| `search` | `string?` | `null` | Tim kiem hop nhat theo ma van don GHN, ma don con (`orderId`), ten hoac SDT nguoi nhan |
| `status` | `string?` | `null` | Loc mot hoac nhieu trang thai van don, phan tach boi dau phay (VD: `ReadyToPick,InTransit`) |
| `sortBy` | `string?` | `createdAt` | Truong sap xep thoi gian (`createdAt` hoac `updatedAt`) |
| `sortOrder` | `string?` | `desc` | Thu tu sap xep (`desc` moi nhat truoc, `asc` cu nhat truoc) |
| `fromDate` | `DateTimeOffset?` | `null` | Tu ngay |
| `toDate` | `DateTimeOffset?` | `null` | Den ngay |
| `page` | `int` | `1` | So trang |
| `pageSize` | `int` | `20` | So ban ghi moi trang (1 - 100) |

## 5. Phan hoi thanh cong (200 OK)
```json
{
  "items": [
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
      "createdAt": "2026-11-21T08:00:00Z",
      "updatedAt": "2026-11-21T09:15:00Z",
      "items": [],
      "events": []
    }
  ],
  "pagination": {
    "pageIndex": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

## 6. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Khong co vai tro hop le.
