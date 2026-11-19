# WH-02: Lay Chi Tiet Kho Hang Theo ID (Get Warehouse By ID)

## 1. Thong tin tong quan
- **Ma chuc nang**: `WH-02`
- **Ten API**: Get Warehouse By ID
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses/{id}`
  - Chay Local: `http://localhost:8006/api/v1/warehouses/{id}`
- **Phan loai**: Public API
- **Quyen han / Role**: Tat ca nguoi dung (Public / Anonymous)

## 2. Mo ta nghiep vu
Tra cuu thong tin chi tiet cua mot kho bai dua tren khoa chinh `id` (UUIDv4):
1. **Truy van du lieu**: Doc kho hang tu bang `warehouses` kem day du cac truong dia danh hanh chinh va GHN Shop ID.
2. **Xu ly khong tim thay**: Neu khong tim thay ban ghi, tra ve HTTP 404 Not Found voi ma loi `WAREHOUSE_NOT_FOUND`.

## 3. Path Parameters
| Tham so | Kieu | Bat buoc | Mo ta |
|---|---|---|---|
| `id` | `Guid` (UUID) | Co | Ma dinh danh kho bai |

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "id": "e6a11e8f-7c15-4fa2-bf42-9a5f4c58cf32",
  "code": "WH-HCM-01",
  "name": "Kho Tong Ho Chi Minh",
  "address": "Khu Cong Nghe Cao, TP. Thu Duc, TP. Ho Chi Minh",
  "contactPhone": "0901234567",
  "provinceName": "TP. Ho Chi Minh",
  "districtId": 1442,
  "wardCode": "20101",
  "ghnShopId": 5550559,
  "isDisabled": false,
  "sellerId": "00000000-0000-0000-0000-000000000000"
}
```

## 5. Phan hoi loi
- **404 Not Found (`WAREHOUSE_NOT_FOUND`)**: Khong tim thay kho hang voi ID cung cap.
- **500 Internal Server Error (`INTERNAL_SERVER_ERROR`)**: Loi he thong khong xac dinh.
