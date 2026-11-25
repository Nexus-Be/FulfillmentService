# SP-02: Danh Muc Quan Huyen Theo Tinh GHN (Get Districts)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SP-02`
- **Ten API**: Get Shipping Location Districts
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/shipping/locations/districts?provinceId={provinceId}`
  - Chay Local: `http://localhost:8006/api/v1/shipping/locations/districts?provinceId={provinceId}`
- **Phan loai**: Public API (Cache in-memory 24h theo tung provinceId)
- **Quyen han / Role**: Tat ca nguoi dung (Public / Anonymous)

## 2. Mo ta nghiep vu
Tra cuu danh sach Quan/Huyen truc thuoc mot Tinh/Thanh pho theo `provinceId`:
- Moi quan huyen tra ve `districtId`, `districtName` va `supportType` de phuc vu tinh phi giao hang.

## 3. Query Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `provinceId` | `int` | Co | Ma tinh thanh (VD: `202` cho TP.HCM) |

## 4. Phan hoi thanh cong (200 OK)
```json
[
  {
    "districtId": 1442,
    "provinceId": 202,
    "districtName": "Quận 1",
    "supportType": 0
  },
  {
    "districtId": 1450,
    "provinceId": 202,
    "districtName": "Quận 7",
    "supportType": 0
  }
]
```

## 5. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: `provinceId <= 0`.

## Nguon du lieu va loi 503 (24/11/2026)
- Danh sach chi den tu GHN, hoac tu mot cau tra loi truoc do cua GHN con trong cache. **Khong con danh sach mau** thay the khi GHN loi hoac thieu token.
- Body van la mang JSON thuan (khong doi hop dong). Nguon va thoi diem GHN tra loi nam trong hai header phan hoi:
  - `X-Location-Source`: `ghn` (vua lay tu GHN) hoac `ghn-cache` (cau tra loi truoc do cua GHN con trong cache).
  - `X-Location-Observed-At`: thoi diem GHN tra loi that, dinh dang ISO 8601.
- Khi khong co du lieu dang tin cay (thieu token, GHN loi, GHN vua loi trong 30 giay truoc): `503` voi `errorCode = LOCATION_SOURCE_UNAVAILABLE`.
- Gateway cho phep trinh duyet doc hai header tren (`WithExposedHeaders`).
