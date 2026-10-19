namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>();
        builder.Property(x => x.Address).HasMaxLength(1000);

        builder.HasMany(w => w.InventoryItems)
            .WithOne(i => i.Warehouse)
            .HasForeignKey(i => i.WarehouseId);

        builder.HasMany(w => w.Shipments)
            .WithOne(s => s.Warehouse)
            .HasForeignKey(s => s.WarehouseId);
    }
}
