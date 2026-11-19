# SP-05: Tinh Cuoc Phi Van Chuyen Tam Tinh (Quote Shipping Fee)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SP-05`
- **Ten API**: Calculate Shipping Fee Quote
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/shipping/quote`
  - Chay Local: `http://localhost:8006/api/v1/shipping/quote`
- **Phan loai**: Public API
- **Quyen han / Role**: Tat ca nguoi dung (Public / Anonymous)

## 2. Mo ta nghiep vu
Tinh toan chi phi van chuyen uoc tinh dua tren trong luong goi hang va dia chi 3 cap:
1. **Tu dong xac dinh kho gui**: Neu `FromDistrictId = 0`, he thong tu dong lay quan huyen tu kho dang hoat dong cua `sellerId`.
2. **Goi API GHN CalculateFee**: Tinh phi chinh xac qua cong GHN `/shiip/public-api/v2/shipping-order/fee`.
3. **Co che Fallback an toan**: Neu mang GHN bi gian doan, he thong tu dong tinh phi du phong (`isEstimatedFallback = true`) dua tren cau hinh khoang cach va trong luong de khong lam gian doan qua trinh mua hang.

## 3. Request Body
```json
{
  "sellerId": "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f",
  "fromDistrictId": 1442,
  "toDistrictId": 1450,
  "toWardCode": "20804",
  "weightInGrams": 500,
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "quantity": 1,
      "weightInGrams": 500
    }
  ]
}
```

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "fee": 28000.0,
  "isEstimatedFallback": false,
  "serviceName": "GHN_STANDARD"
}
```

## 5. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: Thieu `toDistrictId` hoac `toWardCode`.
- **400 Bad Request (`INVALID_FROM_DISTRICT`)**: Khong xac dinh duoc quan huyen kho gui hang.
