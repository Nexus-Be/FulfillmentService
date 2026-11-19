# FUL-13: AI Trich Xuat & Xem Truoc Nhap Kho Tu Excel/CSV (Import Preview)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-13`
- **Ten API**: Inventory Import Preview
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/import-preview`
  - Chay Local: `http://localhost:8006/api/v1/inventories/import-preview`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin` (`[RequireRole("seller", "admin")]`)

## 2. Mo ta nghiep vu
Tiep nhan file Excel (`.xlsx`) hoac `.csv` tu Seller/Admin tai len, su dung bo phan tich thong minh ket hop AI schema mapping de doc danh sach hang hoa nhap kho:
1. **Phan loai du lieu**:
   - `readyToImportItems`: Danh sach cac dong hop le, ma SKU khop voi he thong va so luong hop le.
   - `unmatchedNewItems`: Danh sach cac dong bi loi (sai dinh dang, so luong am hoac ma SKU khong ton tai) de nguoi dung sua lai truoc khi ghi so that su.

## 3. Headers & Form Data
- **Content-Type**: `multipart/form-data`
- **Form fields**:
  - `file`: Tap tin `.xlsx` hoac `.csv`.
  - `warehouseId` (Query): ID kho nhap hang.

## 4. Phan hoi thanh cong (200 OK)
```json
{
  "warehouseId": "b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e",
  "totalRowsRead": 50,
  "readyToImportItems": [
    {
      "rowNumber": 2,
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "skuCode": "SKU-AO-THUN-01",
      "quantity": 100,
      "note": "Nhap kho thang 11",
      "isValid": true,
      "errorMessage": null
    }
  ],
  "unmatchedNewItems": [
    {
      "rowNumber": 15,
      "rawSkuCode": "SKU-UNKNOWN-99",
      "quantity": 20,
      "note": null,
      "reason": "SKU not found in system."
    }
  ]
}
```

## 5. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: Khong co tap tin nao duoc tai len.
- **401 Unauthorized (`UNAUTHORIZED`)**: Thieu Bearer token.
- **403 Forbidden (`FORBIDDEN`)**: Khong so huu kho hang.
