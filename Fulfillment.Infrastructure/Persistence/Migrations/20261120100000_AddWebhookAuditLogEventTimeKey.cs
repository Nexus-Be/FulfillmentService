using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Stores the carrier event time on the webhook audit row so two failed-delivery webhooks with different times are two events.
    /// Idempotent because shared databases do not follow this migration history.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261120100000_AddWebhookAuditLogEventTimeKey")]
    public partial class AddWebhookAuditLogEventTimeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE webhook_audit_logs ADD COLUMN IF NOT EXISTS event_time_key character varying(40) NULL;
CREATE INDEX IF NOT EXISTS idx_webhook_audit_logs_event_key ON webhook_audit_logs (order_code, status, event_time_key);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS idx_webhook_audit_logs_event_key;
ALTER TABLE IF EXISTS webhook_audit_logs DROP COLUMN IF EXISTS event_time_key;");
        }
    }
}
