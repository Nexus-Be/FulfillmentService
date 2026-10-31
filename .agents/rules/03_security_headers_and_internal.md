# 03 — SECURITY HEADERS, INTERNAL API & IDENTITY (FULFILLMENT SERVICE)

## 1. Controller Segregation
* `Fulfillment.Api/Controllers/Public/`: Serves public shipment tracking and warehouse discovery (`ShipmentsController`, `WarehousesController`).
* `Fulfillment.Api/Controllers/Internal/`: Serves inter-service stock initialization (`InternalInventoriesController` at `/api/v1/internal/inventories/initialize`).

## 2. Internal Physical Inventory Protection
* Internal inventory initialization and adjustments are protected by `InternalServiceAuthMiddleware` with header `X-Internal-Service-Key`.
* Never expose direct physical inventory adjustment endpoints to the public internet.

## 3. Trusted Identity Extraction
* Use extension methods in `Fulfillment.Api.Extensions.HttpContextExtensions` to read user/seller identity.
* Warehouse administrative actions require `EnsureAdmin`.
