using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Ensures the indexes the carrier webhook and tracking lookups rely on. A schema comparison found them missing on a shared database.
    /// Idempotent because shared databases do not follow this migration history.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261117090000_EnsureShipmentLookupIndexes")]
    public partial class EnsureShipmentLookupIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS ""IX_shipments_tracking_number"" ON shipments (tracking_number);
CREATE INDEX IF NOT EXISTS ""IX_shipments_ghn_order_code"" ON shipments (ghn_order_code);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Left in place on purpose: other deployments may rely on these indexes.
        }
    }
}
