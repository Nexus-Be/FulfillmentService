# 04 — AUDIT LOGGING & INVENTORY TRACKING (FULFILLMENT SERVICE)

## 1. Mandatory Audit Logging
Record audit logs for critical operations:
* Physical warehouse creation, capacity updates, and deactivation.
* Stock-in and stock-out physical adjustments (variant ID, quantity delta, warehouse ID).
* Shipment tracking creation, carrier assignment, and delivery dispatch.

## 2. Audit Persistence Standards
* Entities inherit `AuditableEntity<Guid>` (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`).
* Stock movements and physical inventory changes are strictly audited via `InventoryLedger` (`inventory_ledger`).
* Carrier webhook callbacks and delivery events are strictly audited via `WebhookAuditLog` (`webhook_audit_logs`) with idempotent deduplication.
* Legacy generic `AuditLogs` table is deprecated and removed; domain entities use native ledgers and webhook logs.
