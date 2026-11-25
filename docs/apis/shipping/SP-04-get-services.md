# SP-04: Goi Cuoc Van Chuyen Kha Dung (Get Available Services)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SP-04`
- **Ten API**: Get Available Shipping Services
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/shipping/services?fromDistrictId={from}&toDistrictId={to}`
  - Chay Local: `http://localhost:8006/api/v1/shipping/services?fromDistrictId={from}&toDistrictId={to}`
- **Phan loai**: Public API
- **Quyen han / Role**: Tat ca nguoi dung (Public / Anonymous)

## 2. Mo ta nghiep vu
Tra cuu cac goi cuoc van chuyen cua GHN ho tro giua Quan/Huyen gui va Quan/Huyen nhan:
- Tra ve danh sach `serviceId`, `shortName` (Chuyen phat chuan, Chuyen phat nhanh...), `serviceTypeId`.

## 3. Query Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `fromDistrictId` | `int` | Co | Ma quan huyen gui hang |
| `toDistrictId` | `int` | Co | Ma quan huyen nhan hang |

## 4. Phan hoi thanh cong (200 OK)
```json
[
  {
    "serviceId": 53320,
    "shortName": "Chuẩn",
    "serviceTypeId": 2
  },
  {
    "serviceId": 53321,
    "shortName": "Nhanh",
    "serviceTypeId": 1
  }
]
```

## Nguon du lieu va loi 503 (25/11/2026)
Danh sach dich vu chi lay tu GHN (hoac cau tra loi truoc do cua GHN con trong cache). **Khong con hai dich vu mau** (ma 53320, 53321) khi GHN loi. Thieu token hoac ma cua hang, GHN loi, hoac GHN tra HTTP 200 ma body khong co `code = 200` kem danh sach: `503` voi `errorCode = LOCATION_SOURCE_UNAVAILABLE`, khong cache.
