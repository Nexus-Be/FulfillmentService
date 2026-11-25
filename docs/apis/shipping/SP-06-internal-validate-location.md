# SP-06: Tham Dinh Dia Danh Hanh Chinh 3 Cap (Validate Shipping Location)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SP-06`
- **Ten API**: Validate Shipping Location Hierarchy
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/shipping/locations/validate`
  - Chay Local: `http://localhost:8006/api/v1/internal/shipping/locations/validate`
- **Phan loai**: Internal API (Goi tu UserService khi luu so dia chi cua buyer, dang ky seller va doi dia chi lay hang; tu CommerceService khi bao gia, doi dia chi nhan va dia chi don dau gia. WarehouseService tu kiem trong cung tien trinh bang cung danh sach, khong qua HTTP)
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

## 6. Nguon du lieu va loi 503 (24/11/2026)
- Viec doi chieu dung cung ba danh sach Tinh/Huyen/Xa nhu SP-01..03, nen chi dung du lieu GHN that hoac cache GHN con han.
- `400 VAL_INVALID_LOCATION` chi tra khi da doi chieu bang du lieu GHN dang tin cay va xac nhan bo ma sai.
- Khi khong co nguon dang tin cay: `503` voi `errorCode = LOCATION_SOURCE_UNAVAILABLE`. Khong ket luan dia chi sai trong truong hop nay.
- Nguoi goi (24/11/2026): UserService (`FulfillmentServiceClient.ValidateLocationAsync`) va CommerceService (`FulfillmentClient.ValidateLocationAsync`, qua `ShippingChain`). Moi nguoi goi coi `400` khong co `errorCode = VAL_INVALID_LOCATION` (vi du loi rang buoc `[Range]` cua request) la khong xac nhan duoc, nen can gui `provinceId`, `districtId` lon hon 0 va `wardCode` khong rong.
