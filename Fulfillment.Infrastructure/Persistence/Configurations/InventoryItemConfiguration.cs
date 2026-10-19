namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.SkuId, x.WarehouseId }).IsUnique();

        builder.HasOne(i => i.Warehouse)
            .WithMany(w => w.InventoryItems)
            .HasForeignKey(i => i.WarehouseId);

        builder.HasMany(i => i.ReservationItems)
            .WithOne(r => r.InventoryItem)
            .HasForeignKey(r => r.InventoryItemId);

        builder.HasMany(i => i.Ledgers)
            .WithOne(l => l.InventoryItem)
            .HasForeignKey(l => l.InventoryItemId);
    }
}
