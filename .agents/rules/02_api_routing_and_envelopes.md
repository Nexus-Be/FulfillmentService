# 02 — API ROUTING, ENVELOPES & PAGINATION (FULFILLMENT SERVICE)

## 1. Route Conventions in FulfillmentService
* Collections: Lowercase plural nouns (`/api/v1/shipments`, `/api/v1/shipments/{id}`, `/api/v1/warehouses`, `/api/v1/warehouses/{id}`, `/api/v1/inventories`).
* Prohibit singular nouns (`/api/v1/shipment`, `/api/v1/warehouse`).
* One route attribute per action. Use single-line XML `<summary>`.

## 2. Pure DTO Return & Zero ResultFilter
* Controllers return pure DTOs:
  * `Ok(shipmentDto)`
  * `StatusCode(StatusCodes.Status201Created, createdWarehouseDto)`
  * `Ok(pagedData)` (for paginated shipments or warehouses)
* Zero ResultFilter: Do NOT apply `ResultFilter` or manual envelope factories in `Fulfillment.Api`. Gateway automatically wraps raw DTOs in `ApiResponse<T>`.

## 3. Database Pagination Governance
* Database-level pagination: `.Skip((page - 1) * pageSize).Take(pageSize)` and `.CountAsync()`.
* Output: Standard `PagedData<T>` envelope containing `items` and `pagination` metadata.
