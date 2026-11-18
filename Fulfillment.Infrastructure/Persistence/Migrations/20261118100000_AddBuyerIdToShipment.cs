using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds buyer_id to shipments and creates an index on it.
    /// Idempotent using IF NOT EXISTS for shared databases.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261118100000_AddBuyerIdToShipment")]
    public partial class AddBuyerIdToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE shipments ADD COLUMN IF NOT EXISTS buyer_id uuid NULL;
CREATE INDEX IF NOT EXISTS ""IX_shipments_buyer_id"" ON shipments (buyer_id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_shipments_buyer_id"";
ALTER TABLE shipments DROP COLUMN IF EXISTS buyer_id;");
        }
    }
}
