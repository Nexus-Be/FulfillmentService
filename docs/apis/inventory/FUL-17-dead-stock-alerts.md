# FUL-17: Danh Sach Canh Bao Hang Ton Dong (Dead Stock Alerts)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-17`
- **Ten API**: Get Dead Stock Alerts
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/alerts/dead-stock`
  - Chay Local: `http://localhost:8006/api/v1/inventories/alerts/dead-stock`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin`, `staff` (`[RequireRole("seller", "admin", "staff")]`)

## 2. Mo ta nghiep vu
Truy van danh sach cac mat hang con ton kho nhung khong phat sinh bat ky giao dich bien dong xuat/nhap nao trong suot N ngay qua (`days`, mac dinh 30 ngay) de giup nguoi ban co ke hoach xa hang hoac giam gia.

## 3. Query Parameters
| Tham so | Kieu | Mac dinh | Mo ta |
|---|---|---|---|
| `warehouseId` | `Guid?` | `null` | Loc theo kho |
| `days` | `int` | `30` | So ngay toi thieu khong phat sinh bien dong |

## 4. Phan hoi thanh cong (200 OK)
```json
[
  {
    "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
    "availableQuantity": 150,
    "totalQuantity": 150,
    "lastMovementAt": "2026-10-15T08:00:00Z",
    "daysWithoutMovement": 35
  }
]
```

## 5. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
