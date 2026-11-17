using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// The GHN webhook writes a row to webhook_audit_logs before it does anything else, but no migration ever created that table
    /// (the entity was added without one), and the table that exists on shared databases lacks the soft-delete and update columns
    /// every entity has. Every webhook therefore failed with 500 and no shipment status could change.
    /// Everything here is idempotent: it creates the table on a fresh database and only adds what is missing on an existing one.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261116150000_EnsureWebhookAuditLogs")]
    public partial class EnsureWebhookAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS webhook_audit_logs (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    order_code character varying(50) NOT NULL,
    client_order_code character varying(100) NULL,
    event_type character varying(50) NULL,
    status character varying(50) NOT NULL,
    raw_json text NULL,
    is_processed boolean NOT NULL DEFAULT false,
    process_message character varying(500) NULL,
    received_at timestamp with time zone NOT NULL DEFAULT now(),
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    is_deleted boolean NOT NULL DEFAULT false,
    deleted_at timestamp with time zone NULL,
    CONSTRAINT webhook_audit_logs_pkey PRIMARY KEY (id)
);

ALTER TABLE webhook_audit_logs ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT now();
ALTER TABLE webhook_audit_logs ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE webhook_audit_logs ADD COLUMN IF NOT EXISTS deleted_at timestamp with time zone NULL;

CREATE INDEX IF NOT EXISTS idx_webhook_audit_logs_order_code ON webhook_audit_logs (order_code);
CREATE INDEX IF NOT EXISTS idx_webhook_audit_logs_received_at ON webhook_audit_logs (received_at);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only the columns this migration may have added. The table itself is left alone because it can predate this migration.
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS webhook_audit_logs DROP COLUMN IF EXISTS deleted_at;
ALTER TABLE IF EXISTS webhook_audit_logs DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS webhook_audit_logs DROP COLUMN IF EXISTS updated_at;");
        }
    }
}
