namespace Fulfillment.Infrastructure.Persistence;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Interfaces;
using Fulfillment.Domain.Base;
using Fulfillment.Domain.Entities;

public class AppDbContext : DbContext, IFulfillmentDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Carrier> Carriers => Set<Carrier>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<InventoryReservationItem> InventoryReservationItems => Set<InventoryReservationItem>();
    public DbSet<InventoryLedger> InventoryLedgers => Set<InventoryLedger>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
    public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();
    public DbSet<WebhookAuditLog> WebhookAuditLogs => Set<WebhookAuditLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        Seeds.FulfillmentSeedData.SeedAll(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDeleteRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditAndSoftDeleteRules();
        return base.SaveChanges();
    }

    private static readonly string[] AuditSkippedProperties = { "UpdatedAt", "CreatedAt", "DeletedAt" };
    private static readonly string[] AuditSensitiveFragments = { "Phone", "Address", "Token", "Receiver" };

    /// <summary>Records shipment and warehouse changes in audit_logs in the same save, so a change cannot exist without its record.</summary>
    private void AddChangeAuditRows()
    {
        var rows = new System.Collections.Generic.List<AuditLog>();
        foreach (var entry in ChangeTracker.Entries().ToList())
        {
            var isShipment = entry.Entity is Shipment;
            var isWarehouse = entry.Entity is Warehouse;
            if (!isShipment && !isWarehouse) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            var visible = entry.Properties.Where(p => !AuditSensitiveFragments.Any(f => p.Metadata.Name.Contains(f, StringComparison.OrdinalIgnoreCase))
                && !AuditSkippedProperties.Contains(p.Metadata.Name)).ToList();

            Fulfillment.Domain.Enums.AuditAction action;
            string? oldValues = null;
            string? newValues = null;
            string? changed = null;

            if (entry.State == EntityState.Added)
            {
                action = isShipment ? Fulfillment.Domain.Enums.AuditAction.ShipmentCreate : Fulfillment.Domain.Enums.AuditAction.WarehouseCreate;
                newValues = System.Text.Json.JsonSerializer.Serialize(visible.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue?.ToString()));
            }
            else if (entry.State == EntityState.Deleted)
            {
                action = Fulfillment.Domain.Enums.AuditAction.WarehouseDelete;
                oldValues = System.Text.Json.JsonSerializer.Serialize(visible.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue?.ToString()));
            }
            else
            {
                var modified = visible.Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)).ToList();
                if (modified.Count == 0) continue;
                var statusChanged = modified.Any(p => p.Metadata.Name == "Status") && isShipment;
                var softDeleted = modified.Any(p => p.Metadata.Name == "IsDeleted" && p.CurrentValue is true);
                action = isShipment
                    ? (statusChanged ? Fulfillment.Domain.Enums.AuditAction.ShipmentStatusChange : Fulfillment.Domain.Enums.AuditAction.ShipmentUpdate)
                    : (softDeleted ? Fulfillment.Domain.Enums.AuditAction.WarehouseDelete : Fulfillment.Domain.Enums.AuditAction.WarehouseUpdate);
                oldValues = System.Text.Json.JsonSerializer.Serialize(modified.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue?.ToString()));
                newValues = System.Text.Json.JsonSerializer.Serialize(modified.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue?.ToString()));
                changed = System.Text.Json.JsonSerializer.Serialize(modified.Select(p => p.Metadata.Name));
            }

            var id = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id")?.CurrentValue?.ToString() ?? string.Empty;
            rows.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ServiceName = "fulfillment-service",
                TraceId = System.Diagnostics.Activity.Current?.TraceId.ToString(),
                UserId = Fulfillment.Application.Common.AuditActor.UserId,
                Action = action,
                EntityName = isShipment ? nameof(Shipment) : nameof(Warehouse),
                EntityId = id,
                OldValues = oldValues,
                NewValues = newValues,
                ChangedColumns = changed,
                CreatedAt = DateTimeOffset.UtcNow,
                IsSynced = false
            });
        }

        if (rows.Count > 0) AuditLogs.AddRange(rows);
    }

    private void ApplyAuditAndSoftDeleteRules()
    {
        var now = DateTimeOffset.UtcNow;
        AddChangeAuditRows();

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }
                if (entry.Entity.UpdatedAt == default)
                {
                    entry.Entity.UpdatedAt = now;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
            }
        }
    }
}
