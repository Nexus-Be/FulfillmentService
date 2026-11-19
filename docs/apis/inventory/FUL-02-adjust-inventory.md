# FUL-02: Nhap Kho Hoac Dieu Chinh Ton Kho (Adjust Inventory)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-02`
- **Ten API**: Adjust Inventory (Single SKU)
- **Phuong thuc**: `POST`
- **URL**:
  - Cong khai: `POST /api/v1/inventories/adjust`
  - Noi bo: `POST /api/v1/internal/inventories/adjust`
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/adjust`
  - Chay Local: `http://localhost:8006/api/v1/inventories/adjust`
- **Phan loai**: Public API / Inter-Service
- **Quyen han / Role**: `seller`, `admin` (`[RequireRole("seller", "admin")]`)

## 2. Mo ta nghiep vu
Thuc hien nhap them hang hoac dieu chinh kiem ke so du ton kho:
1. **Loai dieu chinh (`InventoryAdjustmentType`)**:
   - `Intake` (`1`): Nhap them hang vao kho (`quantity > 0`). He thong cong don so luong vao `TotalQuantity` va `AvailableQuantity`.
   - `Adjust` (`2`): Dieu chinh kiem ke thuc te (So luong `quantity` co the am hoac duong tuy theo so lech thuc te).
2. **Kiem soat ton kho am**: Tuyet doi khong cho phep dieu chinh lam `TotalQuantity < ReservedQuantity` hoac `TotalQuantity < 0` (khong duoc lam anh huong den hang da giu cua khach).
3. **Ghi nhan so cai**: Ghi 1 dong vao bang `InventoryLedgers` voi ma dinh danh `adjustmentLedgerId` duoc tra ve trong ket qua.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Authorization` | `Bearer <JWT_TOKEN>` | Co (doi voi public endpoint) |
| `Content-Type` | `application/json` | Co |

## 4. Request Body
```json
{
  "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "type": "Intake",
  "quantity": 50,
  "note": "Nhap them 50 ao thun dot 2",
  "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e"
}
```

### Chi tiet cac truong:
| Truong | Kieu du lieu | Bat buoc | Mo ta |
|---|---|---|---|
| `skuId` | `Guid` | Co | Ma SKU bien the san pham |
| `type` | `string` / `int` | Co | `Intake` (1) hoac `Adjust` (2) |
| `quantity` | `int` | Co | So luong dieu chinh |
| `note` | `string?` | Khong | Ly do dieu chinh ton kho |
| `warehouseId` | `Guid?` | Khong | Kho hang cu the (neu seller co nhieu kho) |

## 5. Phan hoi thanh cong (200 OK)
```json
{
  "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "totalQuantity": 150,
  "reservedQuantity": 10,
  "availableQuantity": 140,
  "adjustmentLedgerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

## 6. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: Du lieu dau vao thieu hoac sai dinh dang.
- **400 Bad Request (`INVALID_ADJUSTMENT`)**: Dieu chinh lam `TotalQuantity < ReservedQuantity` hoac `TotalQuantity < 0`.
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Nguoi ban khong so huu kho hang.
- **404 Not Found (`INVENTORY_NOT_FOUND`)**: SKU chua duoc khoi tao trong he thong kho.
