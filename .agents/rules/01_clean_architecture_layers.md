# 01 — CLEAN ARCHITECTURE & BOUNDED CONTEXT (FULFILLMENT SERVICE)

## 1. Bounded Context & Port Allocation
* Bounded Context: Physical warehouse management, physical inventory stocks, shipment tracking, carrier shipping rates.
* Service Port: `8006` (HTTP REST API).
* Database: PostgreSQL `fulfillment_db` on port `5438`.

## 2. 4-Layer Clean Architecture
* `Fulfillment.Domain`: Entities (`Warehouse`, `Shipment`, `InventoryItem`), Enums (`ShipmentStatus`, `WarehouseStatus`), Exceptions (`FulfillmentDomainException`).
* `Fulfillment.Application`: Use cases in `Services/{Feature}/`, interface `IFulfillmentDbContext`, request/response records.
* `Fulfillment.Infrastructure`: `AppDbContext`, EF Core configurations, migrations.
* `Fulfillment.Api`: Thin controllers in `Controllers/Public/` and `Controllers/Internal/`, middlewares, extensions, composition root (`Program.cs`).

## 3. Feature Co-Location (Mandatory)
* `Services/Warehouse/`: `IWarehouseService.cs`, `WarehouseService.cs`, `WarehouseRequests.cs`, `WarehouseResponses.cs`.
* `Services/Shipment/`: `IShipmentService.cs`, `ShipmentService.cs`, `ShipmentRequests.cs`, `ShipmentResponses.cs`.
* `Services/Inventory/`: `IInventoryService.cs`, `InventoryService.cs`, `InventoryRequests.cs`, `InventoryResponses.cs`.

## 4. Thin 1-to-1 Controller Pairing
* `WarehousesController.cs` -> injects `IWarehouseService`.
* `ShipmentsController.cs` -> injects `IShipmentService`.
* `InternalInventoriesController.cs` -> injects `IInventoryService` (for internal stock initialization/updates).

## 5. Absolute Architectural Prohibitions
* 🛑 Prohibit Repository Pattern over EF Core (inject `IFulfillmentDbContext` directly).
* 🛑 Prohibit MediatR, gRPC, and Message Brokers.
