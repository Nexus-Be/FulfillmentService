# SP-06: Tham Dinh Dia Danh Hanh Chinh 3 Cap (Validate Shipping Location)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SP-06`
- **Ten API**: Validate Shipping Location Hierarchy
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/shipping/locations/validate`
  - Chay Local: `http://localhost:8006/api/v1/internal/shipping/locations/validate`
- **Phan loai**: Internal API (Goi tu UserService khi luu dia chi nguoi mua hoac WarehouseService khi luu kho seller)
- **Quyen han / Role**: Internal Service

## 2. Mo ta nghiep vu
Kiem tra tinh nhat quan va hop le cua dia chi 3 cap (Tinh/Thanh pho, Quan/Huyen, Phuong/Xa):
- Kiem tra `districtId` co thuc su thuoc ve `provinceId` khong.
- Kiem tra `wardCode` co thuc su thuoc ve `districtId` khong.
- Giup ngan chan loi tao van don that bai tren GHN do nguoi dung chon dia chi khong khop.

## 3. Request Body
```json
{
  "provinceId": 202,
  "districtId": 1442,
  "wardCode": "20101"
}
```

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "isValid": true,
  "provinceId": 202,
  "provinceName": "Hồ Chí Minh",
  "districtId": 1442,
  "districtName": "Quận 1",
  "wardCode": "20101",
  "wardName": "Phường Bến Nghé",
  "message": null
}
```

## 5. Phan hoi loi khi dia chi khong khop (400 Bad Request)
```json
{
  "errorCode": "VAL_INVALID_LOCATION",
  "message": "District with ID '1446' does not belong to province 'Hà Nội'.",
  "data": {
    "isValid": false,
    "provinceId": 201,
    "districtId": 1446,
    "wardCode": "20101",
    "message": "District with ID '1446' does not belong to province 'Hà Nội'."
  }
}
```
