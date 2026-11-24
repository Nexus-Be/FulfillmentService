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

## Nguon du lieu va loi 503 (24/11/2026)
- Danh sach chi den tu GHN, hoac tu mot cau tra loi truoc do cua GHN con trong cache. **Khong con danh sach mau** thay the khi GHN loi hoac thieu token.
- Body van la mang JSON thuan (khong doi hop dong). Nguon va thoi diem GHN tra loi nam trong hai header phan hoi:
  - `X-Location-Source`: `ghn` (vua lay tu GHN) hoac `ghn-cache` (cau tra loi truoc do cua GHN con trong cache).
  - `X-Location-Observed-At`: thoi diem GHN tra loi that, dinh dang ISO 8601.
- Khi khong co du lieu dang tin cay (thieu token, GHN loi, GHN vua loi trong 30 giay truoc): `503` voi `errorCode = LOCATION_SOURCE_UNAVAILABLE`.
- Gateway cho phep trinh duyet doc hai header tren (`WithExposedHeaders`).
