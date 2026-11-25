using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the audit_logs table the central log service pulls from (shipment and warehouse changes).
    /// Idempotent because shared databases do not follow this migration history.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261121090000_AddAuditLogs")]
    public partial class AddAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS audit_logs (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    service_name character varying(100) NOT NULL,
    trace_id character varying(100) NULL,
    user_id character varying(100) NULL,
    user_name character varying(255) NULL,
    action integer NOT NULL,
    entity_name character varying(150) NOT NULL,
    entity_id character varying(150) NOT NULL,
    old_values text NULL,
    new_values text NULL,
    changed_columns text NULL,
    ip_address character varying(50) NULL,
    user_agent character varying(500) NULL,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    is_synced boolean NOT NULL DEFAULT false,
    CONSTRAINT audit_logs_pkey PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS idx_audit_logs_unsynced ON audit_logs (is_synced, created_at);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS audit_logs;");
        }
    }
}
