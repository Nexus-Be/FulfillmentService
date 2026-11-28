namespace Fulfillment.Infrastructure.Persistence.Seeds;

using System;
using Fulfillment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public static class FulfillmentSeedData
{
    public static void SeedAll(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Carrier>().HasData(
            new Carrier
            {
                Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                Code = "GHN",
                Name = "Giao Hàng Nhanh",
                Priority = 0,
                IsActive = true,
                CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                UpdatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                IsDeleted = false
            },
            new Carrier
            {
                Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                Code = "SIMULATOR",
                Name = "Mô phỏng Giao hàng (Simulator)",
                Priority = 10,
                IsActive = true,
                CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                UpdatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                IsDeleted = false
            }
        );
    }
}
