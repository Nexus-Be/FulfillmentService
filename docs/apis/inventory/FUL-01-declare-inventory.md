# FUL-01: Khoi Tao Ton Kho Ban Dau (Initialize Inventory)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-01`
- **Ten API**: Initialize / Declare Inventory
- **Phuong thuc**: `POST`
- **URL**:
  - Cong khai (Public): `POST /api/v1/inventories/initialize`
  - Noi bo (Internal): `POST /api/v1/internal/inventories/initialize` hoac `POST /api/v1/internal/inventories/declare`
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/initialize`
  - Chay Local: `http://localhost:8006/api/v1/inventories/initialize`
- **Phan loai**: Public API / Inter-Service
- **Quyen han / Role**: `seller`, `admin`, `staff` (`[RequireRole("seller", "admin", "staff")]`)

## 2. Mo ta nghiep vu
Khai bao va khoi tao so luong ton kho ban dau cho mot ma SKU bien the san pham tai kho hang vat ly:
1. **Rang buoc duy nhat**: Moi cap `(skuId, warehouseId)` chi duoc phep khoi tao 1 lan duy nhat. Neu SKU da duoc khoi tao trong kho do truoc do, he thong tra ve HTTP 400 `INVENTORY_ALREADY_INITIALIZED`.
2. **Quy tac nghiep vu theo vai tro**:
   - Seller chi duoc phep khoi tao ton kho cho kho do chinh minh so huu.
   - Nhan vien Staff chi duoc phep khoi tao voi so luong ban dau bang `0` (`quantity = 0`). Neu Staff truyen so luong khac 0 se bi chan boi HTTP 400 `VAL_INVALID_INPUT`.
   - Admin co quyen khoi tao bat ky so luong nao cho moi kho.
3. **Ghi nhan so cai**: Khi so luong khoi tao $> 0$, he thong tu dong ghi 1 dong vao So cai kho `InventoryLedger` voi `MovementType = Initial`.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Authorization` | `Bearer <JWT_TOKEN>` | Co (doi voi public endpoint) |
| `Content-Type` | `application/json` | Co |

## 4. Request Body
```json
{
  "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
  "quantity": 100,
  "note": "Khoi tao ton kho ban dau khi tao san pham"
}
```

### Chi tiet cac truong:
| Truong | Kieu du lieu | Bat buoc | Mo ta |
|---|---|---|---|
| `skuId` | `Guid` (UUID) | Co | Ma SKU bien the san pham |
| `warehouseId` | `Guid` (UUID) | Co | Ma kho bai vat ly luu tru hang |
| `quantity` | `int` | Co | So luong khoi tao ($\ge 0$) |
| `note` | `string?` | Khong | Ghi chu nguon goc lo hang |

## 5. Phan hoi thanh cong (200 OK)
```json
{
  "id": "7ca92c12-3214-4112-a123-1c963f66bf11",
  "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
  "totalQuantity": 100,
  "reservedQuantity": 0,
  "availableQuantity": 100,
  "soldQuantity": 0,
  "minStockThreshold": 10,
  "isLowStockAlertEnabled": true,
  "isLowStock": false,
  "lastMovementAt": "2026-11-19T09:00:00Z",
  "updatedAt": "2026-11-19T09:00:00Z"
}
```

## 6. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: Thieu `skuId`, `warehouseId` hoac Staff khoi tao so luong khac 0.
- **400 Bad Request (`INVENTORY_ALREADY_INITIALIZED`)**: SKU da duoc khoi tao trong kho hang nay tu truoc.
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Nguoi ban khong so huu kho hang nay.
- **404 Not Found (`WAREHOUSE_NOT_FOUND`)**: Khong tim thay kho hang.
