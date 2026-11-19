# FUL-05: Lay Lich Su Bien Dong So Cai Kho (Get Inventory Ledgers)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-05`
- **Ten API**: Get Inventory Ledgers (Stock Movement Audit)
- **Phuong thuc**: `GET`
- **URL**:
  - Tong hop co bo loc: `GET /api/v1/inventories/ledgers`
  - Theo 1 SKU: `GET /api/v1/inventories/{skuId}/ledgers`
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/ledgers`
  - Chay Local: `http://localhost:8006/api/v1/inventories/ledgers`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin`, `staff` (`[RequireRole("seller", "admin", "staff")]`)

## 2. Mo ta nghiep vu
Truy van nhat ky ke toan kho (Stock Ledger) theo quy tac **Append-only** (chi ghi them, cam sua/xoa):
1. **Cac loai bien dong (`MovementType`)**:
   - `Initial`: Khoi tao ton kho ban dau.
   - `Intake`: Nhap hang bo sung tu nha cung cap.
   - `Commit`: Xuat kho that su khi don hang da thanh toan/xac nhan.
   - `Release`: Huy giu cho hoan lai ton kho.
   - `Adjust`: Dieu chinh kiem ke thuc te.
   - `Restock`: Hoan hang ve kho khi don hang bi tra lai.
2. **Thong ke tong hop (`Summary`)**: Tinh toan tong so luong nhap (`TotalInboundQuantity`), tong so luong xuat (`TotalOutboundQuantity`), bien dong rong (`NetChange`) va tong so giao dich.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Authorization` | `Bearer <JWT_TOKEN>` | Co |

## 4. Query Parameters
| Tham so | Kieu | Mac dinh | Mo ta |
|---|---|---|---|
| `warehouseId` | `Guid?` | `null` | Loc theo kho hang |
| `skuId` | `Guid?` | `null` | Loc theo SKU |
| `movementType` | `string?` | `null` | Loc loai bien dong (`Intake`, `Commit`...) |
| `referenceType` | `string?` | `null` | Loai chung tu tham chieu (`ORDER`, `AUCTION`...) |
| `referenceId` | `string?` | `null` | Ma chung tu tham chieu |
| `fromDate` | `DateTimeOffset?` | `null` | Tu ngay |
| `toDate` | `DateTimeOffset?` | `null` | Den ngay |
| `search` | `string?` | `null` | Tim kiem tu khoa |
| `page` | `int` | `1` | Trang |
| `pageSize` | `int` | `20` | So ban ghi moi trang |

## 5. Phan hoi thanh cong (200 OK)
```json
{
  "summary": {
    "totalInboundQuantity": 150,
    "totalOutboundQuantity": 10,
    "netChange": 140,
    "totalTransactions": 3
  },
  "items": {
    "items": [
      {
        "id": "11111111-2222-3333-4444-555555555555",
        "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
        "movementType": "Initial",
        "quantity": 100,
        "reservationId": null,
        "referenceType": "MANUAL",
        "referenceId": null,
        "note": "Khoi tao ton kho ban dau",
        "createdAt": "2026-11-01T10:00:00Z",
        "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
        "warehouseName": "Kho Tong Tan Binh"
      },
      {
        "id": "22222222-3333-4444-5555-666666666666",
        "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
        "movementType": "Commit",
        "quantity": 2,
        "reservationId": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
        "referenceType": "ORDER",
        "referenceId": "ORD-999",
        "note": "Xuat kho cho don hang ORD-999",
        "createdAt": "2026-11-01T10:15:00Z",
        "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
        "warehouseName": "Kho Tong Tan Binh"
      }
    ],
    "pagination": {
      "pageIndex": 1,
      "pageSize": 20,
      "totalCount": 2,
      "totalPages": 1,
      "hasPreviousPage": false,
      "hasNextPage": false
    }
  }
}
```

## 6. Phan hoi loi
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Khong co quyen truy cap.
