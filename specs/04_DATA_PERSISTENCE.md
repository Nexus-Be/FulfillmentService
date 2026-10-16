# 04 — MÔ HÌNH THỰC THỂ & CƠ SỞ DỮ LIỆU (FULFILLMENT SERVICE DATA PERSISTENCE)

---

## 1. MÔ HÌNH THỰC THỂ CHÍNH (DOMAIN ENTITIES)

1. **`WarehouseInventory`**:
   - `Id`: `Guid`.
   - `SkuId`: `string` (Unique Index, mã định danh vật lý SKU).
   - `WarehouseCode`: `string` (Mã kho, ví dụ `"WH-HN-01"`).
   - `PhysicalStock`: `int` (Số lượng hàng vật lý thực tế).
   - `WeightGrams`: `int` (Trọng lượng gram).
   - `LocationAisle`: `string` (Vị trí ô kệ trong kho).

2. **`Shipment`**:
   - `Id`: `Guid`.
   - `OrderId`: `Guid` (Đơn hàng tương ứng).
   - `TrackingCode`: `string` (Mã vận đơn duy nhất).
   - `Carrier`: `string` (Đơn vị vận chuyển: `"GiaoHangNhanh"`, `"VNPost"`, `"ViettelPost"`).
   - `SenderAddress`: `string`.
   - `RecipientAddress`: `string`.
   - `ShippingFee`: `decimal`.
   - `Status`: `ShipmentStatus` (`Preparing`, `PickedUp`, `InTransit`, `OutForDelivery`, `Delivered`, `FailedDelivery`).
   - `EstimatedDays`: `int`.
   - `CreatedAt`, `UpdatedAt`: `DateTimeOffset`.
   - Navigation: `ICollection<ShipmentCheckpoint> Checkpoints`.

3. **`ShipmentCheckpoint`**:
   - `Id`: `Guid`.
   - `ShipmentId`: `Guid` (FK $\rightarrow$ `Shipment.Id`).
   - `Status`: `string`.
   - `Location`: `string`.
   - `Description`: `string`.
   - `Timestamp`: `DateTimeOffset`.

---

## 2. QUY CHUẨN CƠ SỞ DỮ LIỆU & EF CORE

- Engine: PostgreSQL.
- Database: `fulfillment_db` tại port `5437`.
- Chuỗi kết nối: `Host=localhost;Port=5437;Database=fulfillment_db;Username=postgres;Password=...`.
- Tên bảng: `warehouse_inventories`, `shipments`, `shipment_checkpoints`.
- **No Repository Pattern**: Tầng Application tương tác trực tiếp với `FulfillmentDbContext`.
