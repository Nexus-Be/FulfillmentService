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

        // 6. Inventory Reservation (for auction item)
        var reservationId = Guid.Parse("e2000000-0000-0000-0000-000000000001");
        modelBuilder.Entity<InventoryReservation>().HasData(
            new InventoryReservation
            {
                Id = reservationId,
                ReferenceType = InventoryReservationReferenceType.AuctionCheckout,
                ReferenceId = "aa000000-0000-0000-0000-000000000001",
                Status = InventoryReservationStatus.Pending,
                ExpiresAt = seedDate.AddDays(7),
                ConfirmedAt = null,
                ReleasedAt = null,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            }
        );

        // 7. Inventory Reservation Item
        modelBuilder.Entity<InventoryReservationItem>().HasData(
            new InventoryReservationItem
            {
                Id = Guid.Parse("e3000000-0000-0000-0000-000000000001"),
                ReservationId = reservationId,
                InventoryItemId = Guid.Parse("e1000000-0000-0000-0000-000000000004"), // NX-HELMET-M
                Quantity = 1,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            }
        );

        // 8. Inventory Ledgers
        modelBuilder.Entity<InventoryLedger>().HasData(
            new InventoryLedger
            {
                Id = Guid.Parse("e4000000-0000-0000-0000-000000000001"),
                InventoryItemId = Guid.Parse("e1000000-0000-0000-0000-000000000004"),
                MovementType = InventoryMovementType.Inbound,
                Quantity = 10,
                ReservationId = null,
                ReferenceType = InventoryLedgerReferenceType.StockIn,
                ReferenceId = "STOCKIN-20260101-001",
                Note = "Khởi tạo tồn kho ban đầu cho Mũ Bảo Hiểm",
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            },
            new InventoryLedger
            {
                Id = Guid.Parse("e4000000-0000-0000-0000-000000000002"),
                InventoryItemId = Guid.Parse("e1000000-0000-0000-0000-000000000004"),
                MovementType = InventoryMovementType.Reserve,
                Quantity = 1,
                ReservationId = reservationId,
                ReferenceType = InventoryLedgerReferenceType.Reservation,
                ReferenceId = "aa000000-0000-0000-0000-000000000001",
                Note = "Khóa tạm tồn kho phục vụ đấu giá",
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            }
        );

        // 9. Shipment Events
        modelBuilder.Entity<ShipmentEvent>().HasData(
            new ShipmentEvent
            {
                Id = Guid.Parse("77200000-0000-0000-0000-000000000001"),
                ShipmentId = shipmentId,
                CarrierStatus = "PICKED_UP",
                Status = ShipmentEventStatus.PickedUp,
                Description = "Bưu tá GHN đã lấy hàng tại kho trung tâm",
                ExternalEventId = "GHN-EVT-001",
                OccurredAt = seedDate.AddHours(2),
                RawPayload = "{\"carrier\":\"GHN\",\"status\":\"PICKED_UP\"}",
                CreatedAt = seedDate.AddHours(2),
                UpdatedAt = seedDate.AddHours(2),
                IsDeleted = false
            },
            new ShipmentEvent
            {
                Id = Guid.Parse("77200000-0000-0000-0000-000000000002"),
                ShipmentId = shipmentId,
                CarrierStatus = "IN_TRANSIT",
                Status = ShipmentEventStatus.InTransit,
                Description = "Đang trung chuyển qua bưu cục Hà Nội",
                ExternalEventId = "GHN-EVT-002",
                OccurredAt = seedDate.AddHours(6),
                RawPayload = "{\"carrier\":\"GHN\",\"status\":\"IN_TRANSIT\"}",
                CreatedAt = seedDate.AddHours(6),
                UpdatedAt = seedDate.AddHours(6),
                IsDeleted = false
            },
            new ShipmentEvent
            {
                Id = Guid.Parse("77200000-0000-0000-0000-000000000003"),
                ShipmentId = shipmentId,
                CarrierStatus = "DELIVERED",
                Status = ShipmentEventStatus.Delivered,
                Description = "Giao hàng thành công tới người nhận",
                ExternalEventId = "GHN-EVT-003",
                OccurredAt = seedDate.AddHours(12),
                RawPayload = "{\"carrier\":\"GHN\",\"status\":\"DELIVERED\"}",
                CreatedAt = seedDate.AddHours(12),
                UpdatedAt = seedDate.AddHours(12),
                IsDeleted = false
            }
        );

        // 10. Audit Log
        modelBuilder.Entity<AuditLog>().HasData(
            new AuditLog
            {
                Id = Guid.Parse("77300000-0000-0000-0000-000000000001"),
                ServiceName = "Fulfillment",
                TraceId = "trace-fulfillment-shipment-001",
                UserId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111").ToString(),
                UserName = "admin@nexus.com",
                Action = AuditAction.ShipmentDeliver,
                EntityName = "Shipment",
                EntityId = shipmentId.ToString(),
                OldValues = "{\"Status\":\"OutForDelivery\"}",
                NewValues = "{\"Status\":\"Delivered\"}",
                ChangedColumns = "[\"Status\"]",
                IpAddress = "127.0.0.1",
                UserAgent = "GHNWebhookService/1.0",
                CreatedAt = seedDate.AddHours(12),
                IsSynced = true
            }
        );
    }
}
