# FUL-14: Xac Nhan Ghi Nhap Kho Tu Ket Qua Import (Batch Commit Import)

## 1. Thong tin tong quan
- **Ma chuc nang**: `FUL-14`
- **Ten API**: Batch Commit Import
- **Phuong thuc**: `POST`
- **URL**:
  - Docker: `http://fulfillment-service:8006/api/v1/inventories/batch-commit-import`
  - Chay Local: `http://localhost:8006/api/v1/inventories/batch-commit-import`
- **Phan loai**: Public API
- **Quyen han / Role**: `seller`, `admin` (`[RequireRole("seller", "admin")]`)

## 2. Mo ta nghiep vu
Sau khi nguoi dung kiem tra man hinh xem truoc (`FUL-13`), bam nut "Xac nhan nhap kho", he thong se thuc hien ghi toan bo cac mat hang hop le vao ton kho va so cai mot cach nguyen tu.
- Su dung cung cau truc request va response voi `POST /api/v1/inventories/batch-adjust` (`FUL-10`).
