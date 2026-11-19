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
