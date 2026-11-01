# FULFILLMENT SERVICE - [HTTP_METHOD] [ROUTE]

> **Feature / Use Case:** {FeatureName}
> **Trạng thái:** ⏳ DRAFT (Pre-Code Plan) | 🛠 IN_PROGRESS | ✅ VERIFIED
> **Ngày tạo:** YYYY-MM-DD
> **Tác giả:** {Author / AI Agent}

---

## 1. Thông tin Định tuyến & RBAC (Routing & Authorization)
- **HTTP Method:** `GET` | `POST` | `PATCH` | `DELETE`
- **Route Endpoint:** `/api/v1/shipments/...` | `/api/v1/warehouses/...` | `/api/v1/inventories/...`
- **Quyền hạn (RBAC Policy):** `Buyer` | `Seller` | `StaffOrAdmin` | `Admin`
- **Quy tắc Sở hữu (Resource Ownership):** `currentUserId == buyerId` hoặc `currentUserId == sellerId` hoặc Admin/Staff bypass.
- **Controller:** `Fulfillment.Api/Controllers/Public/{Feature}Controller.cs`
- **Application Contract:** `Fulfillment.Application/Services/{Feature}/I{Feature}Service.cs`

---

## 2. Đầu vào & Quy chuẩn Validation (Input & Validation Schema)
### 2.1 Route Parameters & Query Parameters
| Tham số | Vị trí | Kiểu C# | Bắt buộc | Ràng buộc / Giới hạn | Ghi chú |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `shipmentId` | Route | `Guid` | Có | UUIDv4 format | Khóa chính vận đơn |
| `page` | Query | `int` | Không | Min: 1, Default: 1 | Tham số phân trang chuẩn |
| `pageSize` | Query | `int` | Không | Range: 1 - 100, Default: 20 | Giới hạn tối đa 100 bản ghi |

### 2.2 Request Body Record DTO
- **Vị trí file:** `Fulfillment.Application/Services/{Feature}/{Feature}Requests.cs`
- **Tên C# Record:** `{ActionName}Request`
- **Khai báo C# & DataAnnotations:**
```csharp
public sealed record {ActionName}Request(
    [Required(ErrorMessage = "Tracking code is required.")]
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Tracking code must be between 5 and 50 characters.")]
    string TrackingCode,

    [Required(ErrorMessage = "Carrier name is required.")]
    string Carrier
);
```
- **JSON Request Body mẫu (camelCase):**
```json
{
  "trackingCode": "VNPOST12345678",
  "carrier": "VNPost"
}
```

---

## 3. Đầu ra & Hợp đồng DTO thuần (Response Contract - Pure DTO)
> **LƯU Ý:** FulfillmentService CHỈ trả về DTO thuần hoặc `PagedData<T>`, KHÔNG bọc qua `ApiResponse<T>`. Vỏ `ApiResponse<T>` sẽ do API Gateway tự động đóng gói tại biên.

- **Vị trí file:** `Fulfillment.Application/Services/{Feature}/{Feature}Responses.cs`
- **Tên C# Record:** `{ActionName}Response` (hoặc `PagedData<{Item}Dto>`)
- **Mã HTTP thành công:** `200 OK` | `201 Created` | `204 NoContent`
- **Khai báo C# Record:**
```csharp
public sealed record {ActionName}Response(
    Guid ShipmentId,
    string TrackingCode,
    string Carrier,
    string Status, // PascalCase enum string
    DateTimeOffset CreatedAt
);
```
- **JSON Response Body mẫu (Pure DTO - camelCase):**
```json
{
  "shipmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "trackingCode": "VNPOST12345678",
  "carrier": "VNPost",
  "status": "InTransit",
  "createdAt": "2026-11-01T14:30:00Z"
}
```

---

## 4. Ma trận Mã lỗi RFC 9110 (Error Matrix)
> **Quy chuẩn:** 100% tiếng Anh, Sentence case cho `message`, SCREAMING_SNAKE_CASE cho `errorCode` (không dùng tiền tố `ERR_`).

| HTTP Status | Error Code | Message (Sentence case) | Điều kiện kích hoạt (Trigger Condition) |
| :---: | :--- | :--- | :--- |
| `400 Bad Request` | `VAL_INVALID_INPUT` | `One or more validation errors occurred.` | Vi phạm DataAnnotations trên request model |
| `401 Unauthorized` | `UNAUTHORIZED` | `Authentication is required to access this resource.` | Thiếu hoặc token không hợp lệ |
| `403 Forbidden` | `FORBIDDEN` | `You do not have permission to access or modify this resource.` | Không thỏa mãn quyền sở hữu vận đơn |
| `404 Not Found` | `SHIPMENT_NOT_FOUND` | `Shipment not found with the specified identifier.` | Không tìm thấy vận đơn trong DB |
| `404 Not Found` | `WAREHOUSE_NOT_FOUND` | `Warehouse not found with the specified identifier.` | Không tìm thấy kho bãi |
| `500 Internal Error` | `INTERNAL_SERVER_ERROR` | `An unexpected error occurred while processing the request.` | Ngoại lệ chưa được xử lý |

### 4.1 Bảng Ánh xạ Enum liên quan (Nếu có)
| Tên Enum C# | Giá trị DB (Integer) | Giá trị API (PascalCase) | Ý nghĩa nghiệp vụ |
| :--- | :---: | :--- | :--- |
| `ShipmentStatus.ReadyToPick` | `0` | `"ReadyToPick"` | Chờ lấy hàng |
| `ShipmentStatus.InTransit` | `1` | `"InTransit"` | Đang vận chuyển |
| `ShipmentStatus.Delivered` | `2` | `"Delivered"` | Đã giao hàng thành công |
| `ShipmentStatus.Failed` | `3` | `"Failed"` | Giao hàng thất bại |

---

## 5. Kế hoạch Thực thi & Checklist Kiểm chứng (Pre-Code Plan & Checklist)
### 5.1 Các bước thực thi nghiệp vụ (Execution Flow Plan)
1. **Bước 1:** Validate tham số đầu vào tại Controller boundary.
2. **Bước 2:** Lấy `currentUserId` từ `HttpContext.GetUserId()` và kiểm tra quyền sở hữu vận đơn.
3. **Bước 3:** Truy vấn entity từ DbContext qua `DbSet<T>`.
4. **Bước 4:** Kiểm tra logic nghiệp vụ, ném Domain Exception nếu vi phạm điều kiện.
5. **Bước 5:** Thực hiện thao tác nghiệp vụ, commit transaction nếu có thay đổi DB.
6. **Bước 6:** Trả về Pure DTO với mã HTTP phù hợp (`200`, `201`, `204`).

### 5.2 Checklist hoàn thành
- [ ] **Pre-Code:** Đã đối soát code có sẵn trong FulfillmentService để tái sử dụng DTO/Enum/Naming.
- [ ] **Pre-Code:** Tài liệu API nội bộ này đã được viết và lưu tại `docs/api/{Feature}/{METHOD}_{ENDPOINT}.md`.
- [ ] **Persistence:** Entity & EF Core Configuration đã hoàn thiện (nếu có thay đổi schema).
- [ ] **Application:** Interface, Service, Request/Response DTOs đã viết xong trong `Services/{Feature}/`.
- [ ] **Api:** Thin Controller đã ghép cặp 1-to-1, trả DTO thuần, XML summary 1 dòng.
- [ ] **Tests:** Đã viết unit/integration tests bao phủ happy path và các mã lỗi trong Error Matrix.
- [ ] **Verify:** `dotnet build Fulfillment.sln -c Release` vượt qua 100% với 0 lỗi.
