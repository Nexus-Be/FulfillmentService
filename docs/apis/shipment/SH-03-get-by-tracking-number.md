# SH-03: Tra Cuu Van Don Theo Ma Van Don (Get By Tracking Number)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SH-03`
- **Ten API**: Get Shipment By Tracking Number
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/shipments/tracking/{trackingNumber}`
  - Chay Local: `http://localhost:8006/api/v1/shipments/tracking/{trackingNumber}`
- **Phan loai**: Public API
- **Quyen han / Role**: `buyer`, `seller`, `admin`, `staff` (`[RequireRole("buyer", "seller", "admin", "staff")]`)

## 2. Mo ta nghiep vu
Tra cuu van don theo ma van don GHN (`trackingNumber`, `ghnOrderCode`, `clientOrderCode`, khong phan biet hoa thuong) hoac theo `SubOrderId` / `ShipmentId` (GUID), bao gom lich su cac moc giao van va anh chung tu giao hang POD (neu co).

## 3. Path Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `trackingNumber` | `string` | Co | Ma van don GHN (VD: `GHN1029384`) |

## 4. Phan hoi thanh cong (200 OK)
Tra ve doi tuong `ShipmentDto` kem danh sach su kien van chuyen `events`.

## 5. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Nguoi dung khong so huu van don.
- **404 Not Found (`SHIPMENT_NOT_FOUND`)**: Khong tim thay ma van don.
