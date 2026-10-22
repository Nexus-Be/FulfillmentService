# 04 — MÔ HÌNH THỰC THỂ & CƠ SỞ DỮ LIỆU (FULFILLMENT SERVICE DATA PERSISTENCE)

---

## 1. MÔ HÌNH THỰC THỂ CHÍNH (DOMAIN ENTITIES)

1. **`Warehouse` (Kho bãi)**:
   - `Id`: `Guid`.
   - `Code`: `string` (Unique Index, mã định danh kho).
   - `Name`: `string`.
   - `Type`: `WarehouseType` (`SellerWarehouse = 0`, `PlatformWarehouse = 1`, `Consignment = 2`).
   - `SellerId`: `Guid?` (Index, Cross-service Ref $\rightarrow$ `UserService.Users.Id`, `null` đối với kho sàn, có giá trị đối với kho riêng của người bán).
   - `Address`: `string?`.
   - `IsDisabled`: `bool`.
   - `CreatedAt`, `UpdatedAt`: `DateTimeOffset`.
   - Navigation: `ICollection<InventoryItem> InventoryItems`, `ICollection<Shipment> Shipments`.

2. **`InventoryItem` (Tồn kho SKU)**:
   - `Id`: `Guid`.
   - `SkuId`: `Guid` (Cross-service Ref $\rightarrow$ `CatalogService.ProductVariants.Id`).
   - `WarehouseId`: `Guid` (FK $\rightarrow$ `Warehouse.Id`).
   - `TotalQuantity`, `ReservedQuantity`, `UnavailableQuantity`, `AvailableQuantity`: `int`.
   - Unique Index: `(SkuId, WarehouseId)`.
   - Navigation: `Warehouse`, `ReservationItems`, `Ledgers`.

3. **`InventoryReservation` & `InventoryReservationItem` (Giữ chỗ tồn kho)**:
   - Quản lý khóa giữ hàng trong thời gian chờ thanh toán đơn hàng thương mại hoặc phiên đấu giá.
   - `ReferenceType`: `InventoryReservationReferenceType` (`Order = 0`, `AuctionCheckout = 1`, `Manual = 2`).
   - `ReferenceId`: `string`.
   - `Status`: `InventoryReservationStatus` (`Pending = 0`, `Confirmed = 1`, `Released = 2`, `Expired = 3`).

4. **`InventoryLedger` (Sổ cái biến động kho)**:
   - `MovementType`: `InventoryMovementType` (`Inbound = 0`, `Outbound = 1`, `Reserve = 2`, `Release = 3`, `Adjustment = 4`, `Return = 5`).
   - `ReferenceType`: `InventoryLedgerReferenceType`.
   - `Quantity`, `Note`.

5. **`Carrier` (Đối tác vận chuyển)**:
   - `Code`: `string` (`"GHN"`, `"VIETTELPOST"`...).
   - `Name`, `Priority`, `IsDisabled`.

6. **`Shipment`, `ShipmentItem`, `ShipmentEvent` (Vận đơn & Hành trình)**:
   - `OrderId`: `Guid` (Cross-service Ref $\rightarrow$ `CommerceService.SubOrders.Id` hoặc `Orders.Id`).
   - `WarehouseId`, `CarrierId`.
   - `Status`: `ShipmentStatus` (`ReadyToPick = 0`, `Picking = 1`, `Packed = 2`, `HandedOver = 3`, `InTransit = 4`, `Delivered = 5`, `DeliveryFailed = 6`, `Returned = 7`, `Cancelled = 8`).
   - `ServiceType`, `TrackingNumber`, `ShippingFee`, `ReceiverName`, `ReceiverPhone`, `ReceiverAddress`.
   - `ShipmentItems`: Chi tiết từng SKU trong kiện hàng.
   - `ShipmentEvents`: Nhật ký các mốc sự kiện hành trình giao nhận.

7. **`AuditLog`**:
   - Ghi vết kiểm toán vận hành kho vận.
