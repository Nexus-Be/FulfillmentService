using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the GHN province id to warehouses so the whole province, district and ward chain can be checked against the GHN lists.
    /// Existing rows keep NULL: the province is never guessed from the name, and such a warehouse is asked to be updated before it is used to ship.
    /// Idempotent because shared databases do not follow this migration history.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261125100000_AddWarehouseProvinceId")]
    public partial class AddWarehouseProvinceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS province_id integer NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE warehouses DROP COLUMN IF EXISTS province_id;");
        }
    }
}
