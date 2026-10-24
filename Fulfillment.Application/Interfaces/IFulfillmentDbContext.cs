namespace Fulfillment.Application.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Domain.Entities;

public interface IFulfillmentDbContext
{
    DbSet<Warehouse> Warehouses { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<InventoryReservation> InventoryReservations { get; }
    DbSet<InventoryReservationItem> InventoryReservationItems { get; }
    DbSet<InventoryLedger> InventoryLedgers { get; }
    DbSet<Carrier> Carriers { get; }
    DbSet<Shipment> Shipments { get; }
    DbSet<ShipmentItem> ShipmentItems { get; }
    DbSet<ShipmentEvent> ShipmentEvents { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
