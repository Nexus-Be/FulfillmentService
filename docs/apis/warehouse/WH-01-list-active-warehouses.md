# WH-01: Lay Danh Sach Kho Dang Hoat Dong (List Active Warehouses)

## 1. Thong tin tong quan
- **Ma chuc nang**: `WH-01`
- **Ten API**: List Active Warehouses
- **Phuong thuc**: `GET`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/warehouses`
  - Chay Local: `http://localhost:8006/api/v1/warehouses`
- **Phan loai**: Public API
- **Quyen han / Role**: Tat ca nguoi dung (Public / Anonymous)

## 2. Mo ta nghiep vu
Truy van danh sach tat ca cac kho bai dang trong trang thai hoat dong (`is_active = true`) co phan trang:
1. **Loc du lieu**: Chi lay cac kho co `IsActive == true`. Cac kho bi vo hieu hoa (`isDisabled = true`) se bi loai khoi danh sach.
2. **Sap xep**: Ket qua duoc sap xep tang dan theo ten kho (`Name ASC`).
3. **Phan trang tai Database**: Tinh toan phan trang qua `Skip((page - 1) * pageSize).Take(pageSize)` va `CountAsync()`, tra ve cau truc `PagedData<WarehouseDto>`.

## 3. Headers
| Header | Gia tri | Bat buoc |
|---|---|---|
| `Accept` | `application/json` | Khong (Mac dinh JSON) |

## 4. Query Parameters
| Tham so | Kieu | Mac dinh | Rang buoc | Mo ta |
|---|---|---|---|---|
| `page` | `int` | `1` | $\ge 1$ | So trang hien tai |
| `pageSize` | `int` | `20` | $1 \le pageSize \le 100$ | So luong phan tu tren mot trang |

## 5. Phan hoi thanh cong (200 OK)
```json
{
  "items": [
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
    },
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "code": "WH-HN-01",
      "name": "Kho Tong Ha Noi",
      "address": "Khu Cong Nghiep Thang Long, Dong Anh, Ha Noi",
      "contactPhone": "0912345678",
      "provinceName": "Ha Noi",
      "districtId": 1450,
      "wardCode": "20804",
      "ghnShopId": null,
      "isDisabled": false,
      "sellerId": null
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
```

## 6. Phan hoi loi
- **500 Internal Server Error (`INTERNAL_SERVER_ERROR`)**: Loi he thong khong xac dinh.
