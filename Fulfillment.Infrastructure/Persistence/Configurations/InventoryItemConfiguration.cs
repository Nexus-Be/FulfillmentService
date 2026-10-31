namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.SkuId).HasColumnName("sku_id").IsRequired();
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
        builder.Property(x => x.TotalQuantity).HasColumnName("total_quantity").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.ReservedQuantity).HasColumnName("reserved_quantity").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.UnavailableQuantity).HasColumnName("unavailable_quantity").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.AvailableQuantity).HasColumnName("available_quantity");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(x => new { x.SkuId, x.WarehouseId }).IsUnique();

        builder.HasOne(i => i.Warehouse)
            .WithMany(w => w.InventoryItems)
            .HasForeignKey(i => i.WarehouseId);
    }
}
