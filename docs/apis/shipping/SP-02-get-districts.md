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
