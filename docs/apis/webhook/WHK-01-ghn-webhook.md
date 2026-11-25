# WHK-01: Tiep Nhan Webhook Doi Trang Thai Tu GHN (Carrier Webhook Callback)

## 1. Thong tin tong quan
- **Ma chuc nang**: `WHK-01`
- **Ten API**: GHN Carrier Webhook Callback
- **Phuong thuc**: `POST`
- **URL**:
  - Chinh thuc: `POST /api/v1/webhooks/ghn`
  - Alias tuong thich: `/webhooks/ghn`, `/api/v1/webhooks/carriers/ghn`, `/api/v1/fulfillment/webhook/ghn`, `/api/v1/fulfillment/webhooks/ghn`
  - Docker: `http://fulfillment-service:8006/api/v1/webhooks/ghn`
  - Gateway Edge: `http://localhost:8080/api/v1/webhooks/ghn`
- **Phan loai**: Carrier Callback Webhook (AllowAnonymous kem Filter bao mat bi mat)
- **Quyen han**: Xac thuc qua token bi mat `X-Webhook-Token` hoac query string `?token=ghn-webhook-secret-key` boi bo loc `[RequireGhnWebhookSecret]`.

## 2. Mo ta nghiep vu & Kien truc xu ly
1. **Bao mat token**: Kiem tra token khop voi cau hinh `Carriers:GHN:WebhookSecret`. Neu sai hoac thieu, tra ve HTTP 401 `WEBHOOK_UNAUTHORIZED`.
2. **Ghi nhat ky kiem toan (`WebhookAuditLogs`)**: Ghi lai raw payload JSON, thoi gian tiep nhan va trang thai.
3. **Co che chong lap (Idempotency Guard)**: Neu cung mot `OrderCode` va `Status` da xu ly thanh cong truoc do, tra ve ngay HTTP 200 `{ "message": "Duplicate webhook event acknowledged.", "status": "delivered" }` ma khong cap nhat lan hai.
4. **Kiem soat tien trinh (Forward-Only State Machine)**:
   - Ap dung `ShipmentStatusTransitions.IsForward` de ngan chan webhook den tre hoac nguoc thu tu lam dao nguoc trang thai van don (vi du: trang thai `delivering` den sau khi da `delivered` se bi bo qua an toan).
5. **Ghi nhan su kien (`ShipmentEvents`)**: Luu tru day du thong tin buu ta (`ShipperName`, `ShipperPhone`), anh chu ky xac nhan giao hang (`PodUrl`), va ma buu cuc kho trung chuyen.
6. **Co che chiu loi Outbox sang Commerce Service (`DeliveryNotificationOutbox`)**:
   - Khi trang thai dat cac moc quan trong (`HandedOver`, `InTransit`, `Delivered`, `DeliveryFailed`, `Returned`, `Cancelled`), he thong dua vao hang doi outbox.
   - Goi sang Commerce Service: `POST /api/v1/sub-orders/{subOrderId}/delivery-status` kem header `X-Internal-Service-Key`.
   - Payload gui Commerce co them `reasonCode` va `reason` (nguyen van GHN gui, doc tu `RawJson` da luu nen lan gui lai giu nguyen). Khong co thi la `null`; khong tu suy ra ly do.
   - Worker nen `CommerceDeliveryNotificationWorker` quet moi 30 giay va tu dong thu lai toi da 12 lan theo co che backoff neu Commerce Service gap su co mang.

## 3. Headers & Query Parameters
| Tham so | Vi tri | Bat buoc | Mo ta |
|---|---|---|---|
| `token` hoac `secret` | Query | Co (neu khong co header) | Bi mat webhook truyen tren URL (VD: `?token=Matkhaumanh` hoac `?secret=Matkhaumanh`) |
| `X-Webhook-Token` | Header | Co (tuy chon thay the) | Bi mat webhook theo chuan GHN v2 |
| `X-Webhook-Secret` | Header | Co (tuy chon thay the) | Bi mat webhook theo form cau hinh `developer.ghn.dev` |
| `X-GHN-Secret` | Header | Co (tuy chon thay the) | Bi mat webhook alias |
| `Content-Type` | Header | Co | `application/json` |

## 3.1. Bang Anh Xa Trang Thai Hai Chieu (GHN <-> Fulfillment <-> Commerce)
| Trang thai GHN Callback | Trang thai Fulfillment (`ShipmentStatus`) | Trang thai Commerce (`SubOrderStatus`) | Hanh vi he thong |
|---|---|---|---|
| `ready_to_pick` | `ReadyToPick` (0) | `Confirmed` (2) | Don hang cho buu ta den kho lay hang |
| `picking` / `money_collect_picking` | `Picking` (1) | `Confirmed` (2) | Buu ta dang den kho lay hang |
| `storing` / `packed` | `Packed` (2) | `Confirmed` (2) | Hang da dong goi tai kho |
| `picked` | `HandedOver` (3) | `Shipped` (3) | Da ban giao cho buu ta GHN |
| `delivering` / `money_collect_delivering` / `sorting` / `transporting` | `InTransit` (4) | `Shipped` (3) | Hang dang luan chuyen hoac tren duong giao |
| `delivered` | `Delivered` (5) | `Delivered` (4) | Giao thanh cong; dong bo `Order` cha neu moi don con deu Delivered |
| `delivery_fail` | `DeliveryFailed` (6) | `Shipped` (3) | Giao that bai lan 1 hoac 2, ghi nhan `DeliveryFailureAttempt` |
| `delivery_fail` (lan 3) | `Returned` (7) | `Cancelled` (6) | Sau 3 lan that bai, tu dong chuyen hoan va huy don, hoan tien vi |
| `waiting_to_return` / `return` / `returning` / `return_transporting` / `return_sorting` | `Returning` (9) | `Returning` (8) | Goi hang dang tren duong tra ve kho nguoi ban |
| `returned` / `return_to_origin` | `Returned` (7) | `Cancelled` (6) | Kho da nhan lai hang; huy don, hoan tien vi, giai phong ton kho |
| `cancel` | `Cancelled` (8) | `Cancelled` (6) | Don hang van chuyen da bi huy tren GHN |
| `exception` / `lost` / `damage` / `scrap` | `DeliveryFailed` (6) | `Shipped` (3) | Su co van chuyen can CSKH ho tro |

## 4. Request Body (GHN Webhook Payload)
```json
{
  "OrderCode": "GHN1029384",
  "Status": "delivered",
  "ClientOrderCode": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "Description": "Da giao hang thanh cong den tay nguoi nhan",
  "Fee": 28000,
  "TotalFee": 28000,
  "ShipperName": "Nguyen Van Buu Ta",
  "ShipperPhone": "0987654321",
  "Warehouse": "Buu cuc Quan 1",
  "POD": "https://cdn.ghn.vn/pod/sign_001.jpg",
  "ConvertedWeight": 500
}
```

## 5. Phan hoi thanh cong (200 OK)
```json
{
  "message": "Webhook processed successfully.",
  "status": "Delivered"
}
```

## 6. Phan hoi khi trung lap (200 OK)
```json
{
  "message": "Duplicate webhook event acknowledged.",
  "status": "delivered"
}
```

## 7. Phan hoi loi
- **400 Bad Request (`VAL_INVALID_INPUT`)**: Thieu `OrderCode` hoac payload khong dung dinh dang.
- **401 Unauthorized (`WEBHOOK_UNAUTHORIZED`)**: Token bi mat khong hop le.
- **503 Service Unavailable (`WEBHOOK_NOT_CONFIGURED`)**: He thong chua cau hinh webhook secret.
