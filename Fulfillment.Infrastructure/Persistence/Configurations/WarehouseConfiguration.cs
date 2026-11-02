namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.SellerId).HasColumnName("seller_id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasMaxLength(20).HasDefaultValue("OWN").IsRequired();
        builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(500);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Ignore(x => x.IsDisabled);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasMany(w => w.InventoryItems)
            .WithOne(i => i.Warehouse)
            .HasForeignKey(i => i.WarehouseId);

        builder.HasMany(w => w.Shipments)
            .WithOne(s => s.Warehouse)
            .HasForeignKey(s => s.WarehouseId);
    }
}
