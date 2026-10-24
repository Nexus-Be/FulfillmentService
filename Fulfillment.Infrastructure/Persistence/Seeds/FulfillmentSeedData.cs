namespace Fulfillment.Infrastructure.Persistence.Seeds;

using Microsoft.EntityFrameworkCore;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;

public static class FulfillmentSeedData
{
    public static void SeedAll(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var centralWarehouseId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var ghnCarrierId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        // 1. Warehouse
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

        // 2. Carrier
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

        // 3. Inventory Items for 5 Catalog SKUs
        modelBuilder.Entity<InventoryItem>().HasData(
            new InventoryItem
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000001"),
                SkuId = Guid.Parse("11100000-0000-0000-0000-000000000001"), // NX-PHONE-256
                WarehouseId = centralWarehouseId,
                TotalQuantity = 100,
                ReservedQuantity = 0,
                UnavailableQuantity = 0,
                AvailableQuantity = 100,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            },
            new InventoryItem
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000002"),
                SkuId = Guid.Parse("22100000-0000-0000-0000-000000000002"), // NX-HOODIE-L
                WarehouseId = centralWarehouseId,
                TotalQuantity = 200,
                ReservedQuantity = 0,
                UnavailableQuantity = 0,
                AvailableQuantity = 200,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            },
            new InventoryItem
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000003"),
                SkuId = Guid.Parse("33100000-0000-0000-0000-000000000003"), // NX-KETTLE-17
                WarehouseId = centralWarehouseId,
                TotalQuantity = 150,
                ReservedQuantity = 0,
                UnavailableQuantity = 0,
                AvailableQuantity = 150,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            },
            new InventoryItem
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000004"),
                SkuId = Guid.Parse("44100000-0000-0000-0000-000000000004"), // NX-HELMET-M
                WarehouseId = centralWarehouseId,
                TotalQuantity = 10,
                ReservedQuantity = 1,
                UnavailableQuantity = 0,
                AvailableQuantity = 9,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            },
            new InventoryItem
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000005"),
                SkuId = Guid.Parse("55100000-0000-0000-0000-000000000005"), // NX-BOOK-ARCH
                WarehouseId = centralWarehouseId,
                TotalQuantity = 500,
                ReservedQuantity = 0,
                UnavailableQuantity = 0,
                AvailableQuantity = 500,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            }
        );

        // 4. Shipment for the completed Commerce SubOrder
        var shipmentId = Guid.Parse("77000000-0000-0000-0000-000000000001");
        modelBuilder.Entity<Shipment>().HasData(
            new Shipment
            {
                Id = shipmentId,
                OrderId = Guid.Parse("02000000-0000-0000-0000-000000000001"), // Commerce SubOrderId
                WarehouseId = centralWarehouseId,
                CarrierId = ghnCarrierId,
                ServiceType = ShippingServiceType.Standard,
                ShippingFee = 0m,
                TrackingNumber = "GHN-NEXUS-20260101-0001",
                Status = ShipmentStatus.Delivered,
                ReceiverName = "John Buyer",
                ReceiverPhone = "0900000004",
                ReceiverAddress = "123 Nguyen Trai, Thanh Xuan, Ha Noi",
                DeliveredAt = seedDate,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            }
        );

        // 5. Shipment Item (Kettle)
        modelBuilder.Entity<ShipmentItem>().HasData(
            new ShipmentItem
            {
                Id = Guid.Parse("77100000-0000-0000-0000-000000000001"),
                ShipmentId = shipmentId,
                SkuId = Guid.Parse("33100000-0000-0000-0000-000000000003"), // NX-KETTLE-17
                Quantity = 1,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            }
        );
    }
}
