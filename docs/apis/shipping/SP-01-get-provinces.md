# SP-01: Danh Muc Tinh Thanh Pho GHN (Get Provinces)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SP-01`
- **Ten API**: Get Shipping Location Provinces
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/shipping/locations/provinces`
  - Chay Local: `http://localhost:8006/api/v1/shipping/locations/provinces`
- **Phan loai**: Public API (co che cache in-memory 24 gio)
- **Quyen han / Role**: Tat ca nguoi dung (Public / Anonymous)

## 2. Mo ta nghiep vu
Tra cuu toan bo danh muc 63 Tinh/Thanh pho truc thuoc trung uong theo chuan ma dinh danh cua GHN Master Data:
- Co che MemoryCache luu tru ket qua trong 24 gio de toi uu toc do phan hoi va tiet kiem quota API cua GHN.

## 3. Phan hoi thanh cong (200 OK)
```json
[
  {
    "provinceId": 201,
    "provinceName": "Hà Nội",
    "code": "HAN"
  },
  {
    "provinceId": 202,
    "provinceName": "Hồ Chí Minh",
    "code": "SGN"
  }
]
```

## Nguon du lieu va loi 503 (24/11/2026)
- Danh sach chi den tu GHN, hoac tu mot cau tra loi truoc do cua GHN con trong cache. **Khong con danh sach mau** thay the khi GHN loi hoac thieu token.
- Body van la mang JSON thuan (khong doi hop dong). Nguon va thoi diem GHN tra loi nam trong hai header phan hoi:
  - `X-Location-Source`: `ghn` (vua lay tu GHN) hoac `ghn-cache` (cau tra loi truoc do cua GHN con trong cache).
  - `X-Location-Observed-At`: thoi diem GHN tra loi that, dinh dang ISO 8601.
- Khi khong co du lieu dang tin cay (thieu token, GHN loi, GHN vua loi trong 30 giay truoc): `503` voi `errorCode = LOCATION_SOURCE_UNAVAILABLE`.
- Gateway cho phep trinh duyet doc hai header tren (`WithExposedHeaders`).
