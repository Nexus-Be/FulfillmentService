using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Creates the snake_case tables the model maps to (carriers, warehouses, shipments...) when they do not exist yet.
    /// InitialCreate builds PascalCase tables (Shipments, Warehouses...) while the model, and every later migration, use the snake_case names,
    /// so on an empty database the next migration failed with 42P01 (relation "shipments" does not exist).
    /// Shared databases already have these tables, so every statement is IF NOT EXISTS and does nothing there.
    /// The GHN carrier is inserted here as well because the InitialCreate seed went into the PascalCase table.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261101000000_EnsureSnakeCaseSchema")]
    public partial class EnsureSnakeCaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS carriers (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    code character varying(30) NOT NULL,
    name character varying(100) NOT NULL,
    priority integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    updated_at timestamp with time zone NOT NULL DEFAULT (now()),
    is_deleted boolean NOT NULL DEFAULT FALSE,
    deleted_at timestamp with time zone,
    CONSTRAINT ""PK_carriers"" PRIMARY KEY (id)
);


CREATE TABLE IF NOT EXISTS inventory_reservations (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    reference_id character varying(100) NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    updated_at timestamp with time zone NOT NULL DEFAULT (now()),
    is_deleted boolean NOT NULL DEFAULT FALSE,
    deleted_at timestamp with time zone,
    CONSTRAINT ""PK_inventory_reservations"" PRIMARY KEY (id)
);


CREATE TABLE IF NOT EXISTS warehouses (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    seller_id uuid,
    code character varying(50) NOT NULL,
    name character varying(200) NOT NULL,
    address character varying(500),
    contact_phone character varying(20),
    province_name character varying(100),
    district_id integer,
    ward_code character varying(20),
    ghn_shop_id integer,
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    updated_at timestamp with time zone NOT NULL DEFAULT (now()),
    is_deleted boolean NOT NULL DEFAULT FALSE,
    deleted_at timestamp with time zone,
    CONSTRAINT ""PK_warehouses"" PRIMARY KEY (id)
);


CREATE TABLE IF NOT EXISTS webhook_audit_logs (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    order_code character varying(50) NOT NULL,
    client_order_code character varying(100),
    event_type character varying(50),
    status character varying(50) NOT NULL,
    raw_json text,
    is_processed boolean NOT NULL DEFAULT FALSE,
    process_message character varying(500),
    received_at timestamp with time zone NOT NULL DEFAULT (now()),
    notify_order_id uuid,
    notify_status character varying(50),
    notify_tracking_number character varying(100),
    notify_description character varying(500),
    commerce_notified_at timestamp with time zone,
    commerce_notify_attempts integer NOT NULL DEFAULT 0,
    commerce_notify_next_retry_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    updated_at timestamp with time zone NOT NULL DEFAULT (now()),
    is_deleted boolean NOT NULL DEFAULT FALSE,
    deleted_at timestamp with time zone,
    CONSTRAINT ""PK_webhook_audit_logs"" PRIMARY KEY (id)
);


CREATE TABLE IF NOT EXISTS inventory_items (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    sku_id uuid NOT NULL,
    warehouse_id uuid NOT NULL,
    total_quantity integer NOT NULL DEFAULT 0,
    reserved_quantity integer NOT NULL DEFAULT 0,
    available_quantity integer,
    sold_quantity integer NOT NULL DEFAULT 0,
    min_stock_threshold integer NOT NULL DEFAULT 10,
    is_low_stock_alert_enabled boolean NOT NULL DEFAULT TRUE,
    last_movement_at timestamp with time zone NOT NULL DEFAULT (now()),
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    updated_at timestamp with time zone NOT NULL DEFAULT (now()),
    is_deleted boolean NOT NULL DEFAULT FALSE,
    deleted_at timestamp with time zone,
    CONSTRAINT ""PK_inventory_items"" PRIMARY KEY (id),
    CONSTRAINT ""FK_inventory_items_warehouses_warehouse_id"" FOREIGN KEY (warehouse_id) REFERENCES warehouses (id) ON DELETE CASCADE
);


CREATE TABLE IF NOT EXISTS shipments (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    order_id uuid NOT NULL,
    buyer_id uuid,
    warehouse_id uuid NOT NULL,
    carrier_id uuid NOT NULL,
    service_type integer NOT NULL,
    shipping_fee numeric(18,2) NOT NULL DEFAULT 0.0,
    tracking_number character varying(100),
    status integer NOT NULL,
    receiver_name character varying(255) NOT NULL,
    receiver_phone character varying(50) NOT NULL,
    receiver_address character varying(1000) NOT NULL,
    to_province_name character varying(100),
    to_district_id integer,
    to_ward_code character varying(20),
    weight integer NOT NULL DEFAULT 500,
    length integer NOT NULL DEFAULT 20,
    width integer NOT NULL DEFAULT 15,
    height integer NOT NULL DEFAULT 10,
    converted_weight integer,
    cod_amount integer NOT NULL DEFAULT 0,
    insurance_value integer NOT NULL DEFAULT 0,
    payment_type_id integer NOT NULL DEFAULT 1,
    required_note integer NOT NULL,
    note character varying(500),
    client_order_code character varying(100),
    ghn_order_code character varying(50),
    print_token character varying(200),
    estimated_delivery_at timestamp with time zone,
    delivered_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    updated_at timestamp with time zone NOT NULL DEFAULT (now()),
    is_deleted boolean NOT NULL DEFAULT FALSE,
    deleted_at timestamp with time zone,
    created_by uuid NOT NULL,
    updated_by uuid NOT NULL,
    CONSTRAINT ""PK_shipments"" PRIMARY KEY (id),
    CONSTRAINT ""FK_shipments_carriers_carrier_id"" FOREIGN KEY (carrier_id) REFERENCES carriers (id) ON DELETE RESTRICT,
    CONSTRAINT ""FK_shipments_warehouses_warehouse_id"" FOREIGN KEY (warehouse_id) REFERENCES warehouses (id) ON DELETE RESTRICT
);


CREATE TABLE IF NOT EXISTS inventory_ledger (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    inventory_item_id uuid NOT NULL,
    movement_type character varying(20) NOT NULL,
    quantity integer NOT NULL,
    reservation_id uuid,
    reference_type character varying(30),
    reference_id character varying(100),
    note character varying(500),
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT ""PK_inventory_ledger"" PRIMARY KEY (id),
    CONSTRAINT ""FK_inventory_ledger_inventory_items_inventory_item_id"" FOREIGN KEY (inventory_item_id) REFERENCES inventory_items (id) ON DELETE CASCADE
);


CREATE TABLE IF NOT EXISTS inventory_reservation_items (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    reservation_id uuid NOT NULL,
    inventory_item_id uuid NOT NULL,
    quantity integer NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    updated_at timestamp with time zone NOT NULL DEFAULT (now()),
    is_deleted boolean NOT NULL DEFAULT FALSE,
    deleted_at timestamp with time zone,
    CONSTRAINT ""PK_inventory_reservation_items"" PRIMARY KEY (id),
    CONSTRAINT ""FK_inventory_reservation_items_inventory_items_inventory_item_~"" FOREIGN KEY (inventory_item_id) REFERENCES inventory_items (id) ON DELETE CASCADE,
    CONSTRAINT ""FK_inventory_reservation_items_inventory_reservations_reservat~"" FOREIGN KEY (reservation_id) REFERENCES inventory_reservations (id) ON DELETE CASCADE
);


CREATE TABLE IF NOT EXISTS shipment_events (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    shipment_id uuid NOT NULL,
    carrier_status character varying(100),
    status integer NOT NULL,
    description character varying(1000),
    external_event_id character varying(100),
    occurred_at timestamp with time zone NOT NULL,
    raw_payload jsonb,
    reason_code character varying(50),
    reason character varying(500),
    shipper_name character varying(100),
    shipper_phone character varying(20),
    pod_url character varying(500),
    warehouse_name character varying(200),
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    updated_at timestamp with time zone NOT NULL DEFAULT (now()),
    is_deleted boolean NOT NULL DEFAULT FALSE,
    deleted_at timestamp with time zone,
    CONSTRAINT ""PK_shipment_events"" PRIMARY KEY (id),
    CONSTRAINT ""FK_shipment_events_shipments_shipment_id"" FOREIGN KEY (shipment_id) REFERENCES shipments (id) ON DELETE CASCADE
);


CREATE TABLE IF NOT EXISTS shipment_items (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    shipment_id uuid NOT NULL,
    sku_id uuid NOT NULL,
    quantity integer NOT NULL,
    name character varying(255) NOT NULL DEFAULT 'Product',
    price numeric(18,2) NOT NULL DEFAULT 0.0,
    weight integer NOT NULL DEFAULT 100,
    length integer NOT NULL DEFAULT 10,
    width integer NOT NULL DEFAULT 10,
    height integer NOT NULL DEFAULT 10,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    updated_at timestamp with time zone NOT NULL DEFAULT (now()),
    is_deleted boolean NOT NULL DEFAULT FALSE,
    deleted_at timestamp with time zone,
    CONSTRAINT ""PK_shipment_items"" PRIMARY KEY (id),
    CONSTRAINT ""FK_shipment_items_shipments_shipment_id"" FOREIGN KEY (shipment_id) REFERENCES shipments (id) ON DELETE CASCADE
);


CREATE UNIQUE INDEX IF NOT EXISTS ""IX_carriers_code"" ON carriers (code);


CREATE UNIQUE INDEX IF NOT EXISTS ""IX_inventory_items_sku_id_warehouse_id"" ON inventory_items (sku_id, warehouse_id);


CREATE INDEX IF NOT EXISTS ""IX_inventory_items_warehouse_id_is_low_stock_alert_enabled_ava~"" ON inventory_items (warehouse_id, is_low_stock_alert_enabled, available_quantity, min_stock_threshold);


CREATE INDEX IF NOT EXISTS ""IX_inventory_items_warehouse_id_last_movement_at"" ON inventory_items (warehouse_id, last_movement_at);


CREATE INDEX IF NOT EXISTS ""IX_inventory_ledger_inventory_item_id_created_at"" ON inventory_ledger (inventory_item_id, created_at);


CREATE INDEX IF NOT EXISTS ""IX_inventory_ledger_reservation_id"" ON inventory_ledger (reservation_id);


CREATE INDEX IF NOT EXISTS ""IX_inventory_reservation_items_inventory_item_id"" ON inventory_reservation_items (inventory_item_id);


CREATE UNIQUE INDEX IF NOT EXISTS ""IX_inventory_reservation_items_reservation_id_inventory_item_id"" ON inventory_reservation_items (reservation_id, inventory_item_id);


CREATE INDEX IF NOT EXISTS ""IX_inventory_reservations_expires_at"" ON inventory_reservations (expires_at);


CREATE UNIQUE INDEX IF NOT EXISTS ""IX_inventory_reservations_reference_id"" ON inventory_reservations (reference_id);


CREATE INDEX IF NOT EXISTS ""IX_shipment_events_shipment_id"" ON shipment_events (shipment_id);


CREATE UNIQUE INDEX IF NOT EXISTS ""IX_shipment_items_shipment_id_sku_id"" ON shipment_items (shipment_id, sku_id);


CREATE INDEX IF NOT EXISTS ""IX_shipments_buyer_id"" ON shipments (buyer_id);


CREATE INDEX IF NOT EXISTS ""IX_shipments_carrier_id"" ON shipments (carrier_id);


CREATE INDEX IF NOT EXISTS ""IX_shipments_ghn_order_code"" ON shipments (ghn_order_code);


CREATE INDEX IF NOT EXISTS ""IX_shipments_order_id"" ON shipments (order_id);


CREATE INDEX IF NOT EXISTS ""IX_shipments_tracking_number"" ON shipments (tracking_number);


CREATE INDEX IF NOT EXISTS ""IX_shipments_warehouse_id"" ON shipments (warehouse_id);


CREATE UNIQUE INDEX IF NOT EXISTS ""IX_warehouses_code"" ON warehouses (code);


CREATE INDEX IF NOT EXISTS idx_webhook_audit_logs_notify_pending ON webhook_audit_logs (commerce_notified_at, commerce_notify_next_retry_at);


CREATE INDEX IF NOT EXISTS ""IX_webhook_audit_logs_order_code"" ON webhook_audit_logs (order_code);


CREATE INDEX IF NOT EXISTS ""IX_webhook_audit_logs_received_at"" ON webhook_audit_logs (received_at);

INSERT INTO carriers (id, code, name, priority, is_active)
VALUES ('ffffffff-ffff-ffff-ffff-ffffffffffff', 'GHN', 'Giao Hàng Nhanh', 1, TRUE)
ON CONFLICT DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Left in place on purpose: shared databases rely on these tables.
        }
    }
}
