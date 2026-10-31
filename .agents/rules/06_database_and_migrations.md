# 06 — DATABASE PERSISTENCE & MIGRATIONS (FULFILLMENT SERVICE)

## 1. Database Isolation
* Dedicated database: `fulfillment_db` on host port `5438`.
* Primary key: 100% `Guid` for entities (`Warehouses`, `Shipments`, `InventoryItems`).
* Shipping fee precision: explicit precision `HasPrecision(18, 2)`.

## 2. Entity Configuration & Soft Delete
* Every entity declares `IsDeleted` and `DeletedAt`. Enforce query filter: `builder.HasQueryFilter(x => !x.IsDeleted)`.
* Foreign keys configured with `DeleteBehavior.Restrict`.

## 3. Migration Safety
* No destructive migrations (`DROP TABLE`, `DROP COLUMN`) without explicit authorization.
* Startup safe auto-migration: `db.Database.Migrate()` wrapped in `try-catch` checking `db.Database.IsRelational()`.
