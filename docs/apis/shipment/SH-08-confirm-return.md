# SH-08: Nguoi Ban Xac Nhan Hang Hoan (Confirm Return)

## 1. Thong tin tong quan
- **Ma chuc nang**: `SH-08`
- **Ten API**: Confirm Return & Restock Inventory
- **Phuong thuc**: `POST`
- **URL**:
  - `POST /api/v1/shipments/orders/{orderId}/confirm-return`
  - Alias: `POST /api/v1/seller/orders/{orderId}/confirm-return`
  - Docker: `http://fulfillment-service:8006/api/v1/shipments/orders/{orderId}/confirm-return`
  - Chay Local: `http://localhost:8006/api/v1/shipments/orders/{orderId}/confirm-return`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin` (`[RequireRole("seller", "admin")]`)

## 2. Mo ta nghiep vu
Nguoi ban xac nhan da nhan lai kien hang hoan tra tu buu ta:
1. **Chuyen trang thai van don**: Cap nhat `Shipment.Status = Returned`.
2. **Tu dong phuc hoi ton kho (Restock)**: Duyet qua tat ca mat hang trong van don, cong tra lai so luong vao bang `InventoryItems` va ghi dong So cai `InventoryLedger` voi `MovementType = Restock`.

## 3. Path Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `orderId` | `Guid` | Co | Ma don hang con (`subOrderId`) |

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "success": true,
  "code": "RETURN_CONFIRMED",
  "message": "Returned package confirmed and inventory restored successfully.",
  "data": {
    "orderId": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
    "restoredStock": 2
  }
}
```

## 5. Phan hoi loi
- **400 Bad Request (`INVALID_SHIPMENT_STATUS`)**: Van don chua o trang thai hoan hang.
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Khong so huu kho xuat hang.
- **404 Not Found (`SHIPMENT_NOT_FOUND`)**: Khong tim thay van don.
