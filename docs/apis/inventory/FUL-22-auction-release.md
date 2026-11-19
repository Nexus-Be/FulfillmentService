# FUL-22: Giai Phong Hang Dau Gia (Auction Release)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-22`
- **Ten API**: Auction Release Inventory
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/internal/fulfillment/auction-release/{referenceId}`
  - Chay Local: `http://localhost:8006/api/v1/internal/fulfillment/auction-release/{referenceId}`
- **Phan loai**: Internal API (Goi tu AuctionService khi phien dau gia ket thuc khong co nguoi mua hoac bi huy)
- **Quyen han / Role**: Internal Service (AuctionService)

## 2. Mo ta nghiep vu
Giai phong hang hoa da bi khoa giu cho trong phien dau gia tro lai ton kho kha dung de tiep tuc kinh doanh tren san.

## 3. Path Parameters & Query Parameters
| Tham so | Vi tri | Kieu | Bat buoc | Mo ta |
|---|---|---|---|---|
| `referenceId` | Path | `string` | Co | Ma phien dau gia (VD: `AUC-SESSION-777`) |
| `reason` | Query | `string?` | Khong | Ly do giai phong (`AUCTION_EXPIRED`, `AUCTION_CANCELLED`) |

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "referenceId": "AUC-SESSION-777",
  "status": "RELEASED",
  "releasedCount": 1
}
```
