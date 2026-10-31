# 05 — ERROR HANDLING & ENUM BOUNDARY (FULFILLMENT SERVICE)

## 1. 100% English & Sentence Case
* Error messages must be 100% English sentence case.
* Capitalize only the first letter. Keep technical acronyms uppercase (`ID`, `URL`, `UUID`).
* Examples: `"Shipment not found with the provided tracking number."`, `"Warehouse is currently inactive."`, `"Warehouse not found with the specified ID."`

## 2. SCREAMING_SNAKE_CASE Error Codes
* Error codes must be uppercase with underscores. No `ERR_` prefixes.
* Use `VAL_` prefix for validation errors.
* Examples: `SHIPMENT_NOT_FOUND`, `WAREHOUSE_INACTIVE`, `WAREHOUSE_NOT_FOUND`, `VAL_INVALID_INPUT`, `UNAUTHORIZED`, `FORBIDDEN`.

## 3. RFC 9110 ProblemDetails Structure
* Handled by `GlobalExceptionHandlerMiddleware.cs`: returns `application/problem+json` containing `type`, `title`, `status`, `errorCode`, `detail`, `traceId`.

## 4. Enum Persistence & Boundary Serialization
* In PostgreSQL (`fulfillment_db`): 100% stored as `integer` (int32). Prohibit `HasConversion<string>()`.
* Enums: `ShipmentStatus` (`Pending = 0`, `ReadyToPick = 1`, `InTransit = 2`, `Delivered = 3`), `WarehouseStatus` (`Active = 1`, `Inactive = 2`).
* REST Outbound: 100% serialized as `PascalCase` strings (`"ReadyToPick"`, `"InTransit"`, `"Delivered"`).
* REST Inbound: Handled via `PascalCaseJsonStringEnumConverter(allowIntegerValues: false)`.
