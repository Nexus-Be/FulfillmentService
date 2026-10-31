namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
{
    public void Configure(EntityTypeBuilder<ShipmentItem> builder)
    {
        builder.ToTable("shipment_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.ShipmentId).HasColumnName("shipment_id").IsRequired();
        builder.Property(x => x.SkuId).HasColumnName("sku_id").IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();

        builder.HasIndex(x => new { x.ShipmentId, x.SkuId }).IsUnique();

        builder.HasOne(i => i.Shipment)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.ShipmentId);
    }
}
