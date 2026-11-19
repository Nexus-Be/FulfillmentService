# SH-04: Tra Cuu Van Don Theo Don Hang Con (Get By Order ID)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SH-04`
- **Ten API**: Get Shipment By Order ID
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/shipments/order/{orderId}`
  - Chay Local: `http://localhost:8006/api/v1/shipments/order/{orderId}`
- **Phan loai**: Public API
- **Quyen han / Role**: `buyer`, `seller`, `admin`, `staff` (`[RequireRole("buyer", "seller", "admin", "staff")]`)

## 2. Mo ta nghiep vu
Tra cuu van don gan lien voi ma dinh danh don hang con (`subOrderId`).

## 3. Path Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `orderId` | `Guid` (UUID) | Co | Ma dinh danh don hang con (`subOrderId`) |

## 4. Phan hoi thanh cong (200 OK)
Tra ve doi tuong `ShipmentDto`.

## 5. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Nguoi dung khong so huu don hang nay.
- **404 Not Found (`SHIPMENT_NOT_FOUND`)**: Chua co van don duoc tao cho don hang con nay.
