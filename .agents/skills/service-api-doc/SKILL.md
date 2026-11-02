---
name: service-api-doc
description: Pre-code API documentation standard for FulfillmentService (port 8006). Mandates drafting a service-level technical API doc before coding, referencing existing domain entities, enums, DTOs, and error patterns for system-wide consistency. Governs pure DTO contracts, validation attributes, and RFC 9110 error matrices inside FulfillmentService.
---

# FulfillmentService API Pre-Code Documentation Skill

> **Scope:** FulfillmentService repository only (`src/Services/Fulfillment/`). 100% Inward-Only. Never reference the root workspace or sibling services.

This skill governs the **mandatory pre-code documentation lifecycle** within FulfillmentService. Before any C# code is written or modified for a REST endpoint, the developer or AI agent must create a technical API specification inside `docs/api/`.

---

## 1. Ranh giới Tài liệu FulfillmentService

* **Vị trí lưu trữ:** `docs/api/{Feature}/{HTTP_METHOD}_{ENDPOINT_SLUG}.md` (ví dụ `docs/api/Shipments/GET_SHIPMENTS.md`).
* **Thời điểm lập:** **BẮT BUỘC TRƯỚC KHI VIẾT CODE (Pre-Code)**.
* **Định dạng dữ liệu trả về:** **Pure DTO** hoặc `PagedData<T>` (tuyệt đối không bọc qua `ApiResponse<T>`).
* **Rào chắn Inward-Only:** Chỉ trỏ tới tài nguyên nội bộ của FulfillmentService (`Fulfillment.Domain`, `Fulfillment.Application`, `Fulfillment.Infrastructure`, `Fulfillment.Api`). Không tham chiếu tới `docs/api/` ở Root.

---

## 2. Quy trình 3 Bước Tra cứu Source of Truth Trước Khi Lập Doc

### Bước 1: Tra cứu Thực thể Domain (`Fulfillment.Domain/Entities/`)
- Mở `Fulfillment.Domain/Entities/` (ví dụ `Warehouse.cs`, `Shipment.cs`, `InventoryItem.cs`).
- Ràng buộc: Tracking number format, trọng lượng, kích thước, và số lượng tồn kho.

### Bước 2: Tra cứu Bảng mã Enum (`Fulfillment.Domain/Enums/`)
- Mở `Fulfillment.Domain/Enums/` (ví dụ `ShipmentStatus.cs`, `WarehouseStatus.cs`).
- Ghi nhận số nguyên DB (`0`, `1`, `2`...) và chuỗi PascalCase API (`ReadyToPick`, `InTransit`, `Delivered`, `Failed`).
- Điền bảng ánh xạ vào Mục 4.1 của doc.

### Bước 3: Tra cứu Mẫu DTO & Xử lý Lỗi Có Sẵn (`Fulfillment.Application/`)
- Rà soát các tính năng có sẵn tại `Fulfillment.Application/Services/` (ví dụ `Shipments/`, `Warehouses/`).
- Kế thừa quy ước đặt tên DTO: `{Action}Request`, `{Action}Response`.
- Kế thừa mã lỗi đã có (`SHIPMENT_NOT_FOUND`, `WAREHOUSE_INACTIVE`, `VAL_INVALID_INPUT`).

---

## 3. Các Rào Chắn Bất Biến tại FulfillmentService

1. **Pure DTO:** Controller tại FulfillmentService trả về DTO thuần, cấm thêm `ResultFilter` hay bọc `ApiResponse<T>`.
2. **Plural Route:** `/api/v1/shipments`, `/api/v1/warehouses`, `/api/v1/inventories`.
3. **Mã lỗi & Message:** 100% tiếng Anh, Sentence case cho `message`, SCREAMING_SNAKE_CASE cho `errorCode` (cấm tiền tố `ERR_`).
4. **Bảo mật vận đơn:** Tra cứu chi tiết vận đơn chỉ dành cho chủ đơn hàng, người bán gửi hàng hoặc Admin/Staff.
5. **Phân trang tại DB:** Toàn bộ danh sách phân trang tính toán tại DB qua `Skip`/`Take` và `CountAsync`, trả về `PagedData<T>`.

---

## 4. Quy trình Thực hiện (Workflow)

1. Sao chép `docs/api/SERVICE_API_DOC_TEMPLATE.md` vào `docs/api/{Feature}/{HTTP_METHOD}_{ENDPOINT_SLUG}.md`.
2. Điền đầy đủ 5 phần của template dựa trên kết quả tra cứu Source of Truth.
3. Hoàn thiện mục 5.1 (Pre-Code Plan) mô tả các bước xử lý kỹ thuật.
4. Triển khai code C# tuân thủ 100% hợp đồng đã đặc tả trong doc.
5. Chạy `dotnet test Fulfillment.sln -c Release` (hoặc build `Fulfillment.sln`) đạt 100% pass, tích checklist và chuyển trạng thái doc sang `✅ VERIFIED`.
