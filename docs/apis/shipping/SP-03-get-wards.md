# SP-03: Danh Muc Phuong Xa Theo Huyen GHN (Get Wards)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SP-03`
- **Ten API**: Get Shipping Location Wards
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/shipping/locations/wards?districtId={districtId}`
  - Chay Local: `http://localhost:8006/api/v1/shipping/locations/wards?districtId={districtId}`
- **Phan loai**: Public API (Cache in-memory 24h theo tung districtId)
- **Quyen han / Role**: Tat ca nguoi dung (Public / Anonymous)

## 2. Mo ta nghiep vu
Tra cuu danh sach Phuong/Xa truc thuoc Quan/Huyen theo `districtId`:
- Tra ve `wardCode` (chuoi ky tu bat buoc truyen cho GHN khi tao don hoac tinh phi) va `wardName`.

## 3. Query Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `districtId` | `int` | Co | Ma quan huyen (VD: `1442`) |

## 4. Phan hoi thanh cong (200 OK)
```json
[
  {
    "wardCode": "20101",
    "districtId": 1442,
    "wardName": "Phường Bến Nghé"
  },
  {
    "wardCode": "20102",
    "districtId": 1442,
    "wardName": "Phường Bến Thành"
  }
]
```

## 5. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: `districtId <= 0`.
