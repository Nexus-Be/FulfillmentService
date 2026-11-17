using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the outbox columns that keep a delivery update owed to CommerceService until Commerce acknowledges it.
    /// Idempotent, like EnsureWebhookAuditLogs, because shared databases do not follow this migration history.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261116160000_AddWebhookAuditLogNotifyOutbox")]
    public partial class AddWebhookAuditLogNotifyOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE webhook_audit_logs ADD COLUMN IF NOT EXISTS notify_order_id uuid NULL;
ALTER TABLE webhook_audit_logs ADD COLUMN IF NOT EXISTS notify_status character varying(50) NULL;
ALTER TABLE webhook_audit_logs ADD COLUMN IF NOT EXISTS notify_tracking_number character varying(100) NULL;
ALTER TABLE webhook_audit_logs ADD COLUMN IF NOT EXISTS notify_description character varying(500) NULL;
ALTER TABLE webhook_audit_logs ADD COLUMN IF NOT EXISTS commerce_notified_at timestamp with time zone NULL;
ALTER TABLE webhook_audit_logs ADD COLUMN IF NOT EXISTS commerce_notify_attempts integer NOT NULL DEFAULT 0;
ALTER TABLE webhook_audit_logs ADD COLUMN IF NOT EXISTS commerce_notify_next_retry_at timestamp with time zone NULL;
CREATE INDEX IF NOT EXISTS idx_webhook_audit_logs_notify_pending ON webhook_audit_logs (commerce_notified_at, commerce_notify_next_retry_at);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS idx_webhook_audit_logs_notify_pending;
ALTER TABLE IF EXISTS webhook_audit_logs DROP COLUMN IF EXISTS commerce_notify_next_retry_at;
ALTER TABLE IF EXISTS webhook_audit_logs DROP COLUMN IF EXISTS commerce_notify_attempts;
ALTER TABLE IF EXISTS webhook_audit_logs DROP COLUMN IF EXISTS commerce_notified_at;
ALTER TABLE IF EXISTS webhook_audit_logs DROP COLUMN IF EXISTS notify_description;
ALTER TABLE IF EXISTS webhook_audit_logs DROP COLUMN IF EXISTS notify_tracking_number;
ALTER TABLE IF EXISTS webhook_audit_logs DROP COLUMN IF EXISTS notify_status;
ALTER TABLE IF EXISTS webhook_audit_logs DROP COLUMN IF EXISTS notify_order_id;");
        }
    }
}
