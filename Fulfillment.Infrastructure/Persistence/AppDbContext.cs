namespace Fulfillment.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Fulfillment.Domain.Entities;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<InventoryReservationItem> InventoryReservationItems => Set<InventoryReservationItem>();
    public DbSet<InventoryLedger> InventoryLedgers => Set<InventoryLedger>();
    public DbSet<Carrier> Carriers => Set<Carrier>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
    public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

public class FulfillmentDbContext : AppDbContext
{
    public FulfillmentDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
