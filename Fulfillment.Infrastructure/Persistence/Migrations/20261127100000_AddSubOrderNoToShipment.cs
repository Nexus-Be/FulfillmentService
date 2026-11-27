using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds sub_order_no to shipments and creates an index on it so shipments can be looked up by human-readable order number.
    /// Idempotent using IF NOT EXISTS for shared databases.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261127100000_AddSubOrderNoToShipment")]
    public partial class AddSubOrderNoToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE shipments ADD COLUMN IF NOT EXISTS sub_order_no character varying(100) NULL;
CREATE INDEX IF NOT EXISTS ""IX_shipments_sub_order_no"" ON shipments (sub_order_no);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_shipments_sub_order_no"";
ALTER TABLE shipments DROP COLUMN IF EXISTS sub_order_no;");
        }
    }
}
