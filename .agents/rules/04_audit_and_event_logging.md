# 04 — AUDIT LOGGING & INVENTORY TRACKING (FULFILLMENT SERVICE)

## 1. Mandatory Audit Logging
Record audit logs for critical operations:
* Physical warehouse creation, capacity updates, and deactivation.
* Stock-in and stock-out physical adjustments (variant ID, quantity delta, warehouse ID).
* Shipment tracking creation, carrier assignment, and delivery dispatch.

## 2. Audit Persistence Standards
* Entities inherit `AuditableEntity<Guid>` (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`).
* Audit rows are written into this service's own `AuditLogs` table through `_dbContext.AddAuditLog(...)` (`Fulfillment.Application/Common/Extensions/AuditLogExtensions.cs`).
* The audit row is committed in the same database transaction as the business change.
* Never instantiate `new AuditLog { ... }` outside the extension. Never replace an audit row with `_logger.LogInformation`.
* Never call `LogService` over HTTP inside the business transaction. Rows start with `IsSynced = false`; a background sync may later forward them to `POST /api/v1/internal/logs/batch` with `X-Internal-Service-Key`.
* Audit rows are append-only. INSERT and SELECT only, never UPDATE or DELETE.
* Field reference: `ServiceName`, `Action`, `EntityName`, `EntityId`, `UserId`, `UserName`, `OldValues`, `NewValues`, `ChangedColumns`, `TraceId`, `IpAddress`, `UserAgent`, `CreatedAt`, `IsSynced`.
