# FUL-21: Khoa Giu Cho Hang Dau Gia (Auction Reserve)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-21`
- **Ten API**: Auction Reserve Inventory
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/fulfillment/auction-reserve`
  - Chay Local: `http://localhost:8006/api/v1/internal/fulfillment/auction-reserve`
- **Phan loai**: Internal API (Goi tu AuctionService khi bat dau phien dau gia truc tiep)
- **Quyen han / Role**: Internal Service (AuctionService)

## 2. Mo ta nghiep vu
Khoa giu cho san pham vat ly de dua vao phien dau gia truc tiep (Live Auction):
- Dam bao san pham khong bi ban trung tren san thuong mai trong thoi gian phien dau gia dang dien ra.
- Khoi tao phieu giu cho co thoi han tuong ung voi thoi gian dien ra phien dau gia.

## 3. Request Body
```json
{
  "referenceId": "AUC-SESSION-777",
  "holdMinutes": 1440,
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "quantity": 1
    }
  ]
}
```

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "id": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
  "referenceId": "AUC-SESSION-777",
  "expiresAt": "2026-11-20T10:00:00Z",
  "createdAt": "2026-11-19T10:00:00Z",
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "quantity": 1
    }
  ]
}
```
