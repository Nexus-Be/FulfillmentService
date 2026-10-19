namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServiceType).HasConversion<string>();
        builder.Property(x => x.ShippingFee).HasPrecision(18, 2);
        builder.Property(x => x.TrackingNumber).HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.ReceiverName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ReceiverPhone).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReceiverAddress).HasMaxLength(1000).IsRequired();

        builder.HasOne(s => s.Warehouse)
            .WithMany(w => w.Shipments)
            .HasForeignKey(s => s.WarehouseId);

        builder.HasOne(s => s.Carrier)
            .WithMany(c => c.Shipments)
            .HasForeignKey(s => s.CarrierId);

        builder.HasMany(s => s.Items)
            .WithOne(i => i.Shipment)
            .HasForeignKey(i => i.ShipmentId);

        builder.HasMany(s => s.Events)
            .WithOne(e => e.Shipment)
            .HasForeignKey(e => e.ShipmentId);
    }
}
