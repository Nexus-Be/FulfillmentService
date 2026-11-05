namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
{
    public void Configure(EntityTypeBuilder<ShipmentItem> builder)
    {
        builder.ToTable("ShipmentItems");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ShipmentId, x.SkuId }).IsUnique();

        builder.HasOne(i => i.Shipment)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.ShipmentId);
    }
}
