namespace Fulfillment.Infrastructure.Persistence.Seeds;

using Microsoft.EntityFrameworkCore;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;

public static class FulfillmentSeedData
{
    public static void SeedCarriersAndWarehouses(ModelBuilder modelBuilder)
    {
        var centralWarehouseId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var ghnCarrierId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var seedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        modelBuilder.Entity<Warehouse>().HasData(
            new Warehouse
            {
                Id = centralWarehouseId,
                Code = "WH-CENTRAL-01",
                Name = "Kho Trung Tâm Sàn Nexus",
                Type = WarehouseType.PlatformWarehouse,
                Address = "Hà Nội, Việt Nam",
                IsDisabled = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            }
        );

        modelBuilder.Entity<Carrier>().HasData(
            new Carrier
            {
                Id = ghnCarrierId,
                Code = "GHN",
                Name = "Giao Hàng Nhanh",
                Priority = 1,
                IsDisabled = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            }
        );
    }
}
