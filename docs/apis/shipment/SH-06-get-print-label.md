# SH-06: Lay Token In Nhan Van Don Nhiet (Get Print Label)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SH-06`
- **Ten API**: Get Print Label
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/shipments/{id}/print-label`
  - Chay Local: `http://localhost:8006/api/v1/shipments/{id}/print-label`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin`, `staff` (`[RequireRole("seller", "admin", "staff")]`)

## 2. Mo ta nghiep vu
Tao va tra ve print token kem duong dan in van don truc tiep tu may chu GHN:
- Ho tro cac kho giay in nhiet: `A5`, `80x80`, `52x70`.
- URL in: `https://dev-online-gateway.ghn.vn/a5/public-api/print{pageSize}?token={printToken}`.

## 3. Path & Query Parameters
| Tham so | Vi tri | Kieu | Bat buoc | Mo ta |
|---|---|---|---|---|
| `id` | Path | `Guid` | Co | Ma van don |
| `pageSize` | Query | `string` | Khong | Kho giay: `A5` (mac dinh), `80x80`, `52x70` |

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "shipmentId": "e0b1c2d3-e4f5-4a6b-8c9d-0e1f2a3b4c5d",
  "trackingNumber": "GHN1029384",
  "token": "mock_ghn_print_token_xyz",
  "printUrl": "https://dev-online-gateway.ghn.vn/a5/public-api/printA5?token=mock_ghn_print_token_xyz",
  "pageSize": "A5"
}
```

## 5. Phan hoi loi
- **400 Bad Request (`INVALID_SHIPMENT_STATUS`)**: Van don da bi huy khong the in nhan.
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Khong so huu van don.
- **404 Not Found (`SHIPMENT_NOT_FOUND`)**: Khong tim thay van don.
