# FULFILLMENT - POST /api/v1/internal/shipments

> **Feature / Use Case:** Tạo vận đơn giao hàng kết nối GHN Staging (Create Shipment)
> **Trạng thái:** ✅ VERIFIED
> **Ngày tạo:** 2026-11-25
> **Tác giả:** Antigravity AI Agent

---

## 1. Thông tin Định tuyến & RBAC (Routing & Authorization)
- **HTTP Method:** `POST`
- **Route Endpoint:**
  - Nội bộ S2S (Commerce -> Fulfillment): `/api/v1/internal/shipments`
  - Controller: `Fulfillment.Api/Controllers/Internal/InternalShipmentsController.cs`
- **Quyền hạn (RBAC Policy):** S2S Service Client (`CommerceService`)
- **Application Contract:** `Fulfillment.Application/Services/Shipment/IShipmentService.cs`

---

## 2. Đầu vào & Quy chuẩn Validation (Input & Validation Schema)

### 2.1 Request Body Record DTO
- **Vị trí file:** `Fulfillment.Application/Services/Shipment/ShipmentRequests.cs`
- **Tên C# Record:** `CreateShipmentRequest`
```csharp
public sealed record CreateShipmentRequest(
    [Required] Guid OrderId,
    [Required] Guid SubOrderId,
    [Required] Guid SellerId,
    [Required] string RecipientName,
    [Required] string RecipientPhone,
    [Required] string ShippingAddress,
    Guid? BuyerId = null,
    string? ToProvinceName = null,
    int? ToDistrictId = null,
    string? ToWardCode = null,
    int WeightInGrams = 500,
    int LengthCm = 20,
    int WidthCm = 15,
    int HeightCm = 10,
    int CodAmount = 0,
    int InsuranceValue = 0,
    string? RequiredNote = "CHOXEMHANGKHONGTHU",
    string? Note = null,
    IReadOnlyList<CreateShipmentItemRequest>? Items = null);
```

- **JSON Request Body mẫu (camelCase):**
```json
{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "subOrderId": "c2f9d801-44bf-4dc7-84ec-86f7b9492651",
  "sellerId": "d1e2f3a4-b5c6-4d7e-8f9a-0b1c2d3e4f5a",
  "recipientName": "Quốc Hoàng",
  "recipientPhone": "0987654321",
  "shippingAddress": "123 Hai Bà Trưng, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
  "toDistrictId": 1442,
  "toWardCode": "20101",
  "weightInGrams": 500,
  "lengthCm": 20,
  "widthCm": 15,
  "heightCm": 10,
  "codAmount": 0,
  "requiredNote": "CHOXEMHANGKHONGTHU",
  "items": [
    {
      "skuId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "quantity": 1,
      "name": "Nexus Test Product",
      "price": 100000,
      "weightInGrams": 500
    }
  ]
}
```

---

## 3. Đầu ra & Hợp đồng DTO thuần (Response Contract - Pure DTO)
- **Tên C# Record:** `CreateShipmentResponse`
- **Mã HTTP thành công:** `200 OK`
- **JSON Response Body mẫu (camelCase):**
```json
{
  "id": "e4f5a6b7-c8d9-40e1-b2c3-d4e5f6a7b8c9",
  "trackingNumber": "LY9KPR",
  "labelUrl": "https://fulfillment.nexus.internal/labels/LY9KPR.pdf",
  "status": "ReadyToPick"
}
```

---

## 4. Ma trận Mã lỗi & Ngoại lệ (RFC 9110 Error Matrix)
| HTTP Status | Mã lỗi (`SCREAMING_SNAKE_CASE`) | Thông báo lỗi chuẩn (`Sentence case`) | Điều kiện kích hoạt |
| :---: | :--- | :--- | :--- |
| `400` | `VAL_INVALID_INPUT` | "Invalid request payload." | Thiếu các trường bắt buộc (`OrderId`, `SubOrderId`, `SellerId`, thông tin người nhận). |
| `400` | `WAREHOUSE_ADDRESS_INCOMPLETE` | "The warehouse address is not complete or has not been confirmed yet, so no shipment can be created." | Kho người bán thiếu địa chỉ bưu cục GHN hợp lệ. |
| `404` | `WAREHOUSE_NOT_FOUND` | "Active warehouse of seller was not found." | Không tìm thấy kho hoạt động của Seller. |
| `502` | `CARRIER_ERROR` | "GHN communication error." | Lỗi mạng hoặc GHN Staging trả mã lỗi nghiệp vụ. |
| `503` | `CARRIER_TIMEOUT` | "GHN fee calculation timed out." | GHN không phản hồi trong khoảng thời gian timeout (10s). |

---

## 5. Danh mục Kiểm tra Hoàn tất (Completion Checklist)
- [x] Application Layer: `IShipmentService.CreateShipmentAsync` tích hợp `GhnGateway.CreateShipmentAsync`.
- [x] API Layer: `InternalShipmentsController.CreateShipment` (`POST /api/v1/internal/shipments`).
- [x] Automated Testing: Unit tests & API tests đạt 100% PASS (`ShipmentServiceTests.cs`).
- [x] Live Verified: Tạo đơn hàng thực tế thành công trên GHN Staging (`LY9KPR` trên `https://5sao.ghn.dev/manage-order`).
- [x] Status: **COMPLETED**
