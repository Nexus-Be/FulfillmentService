namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
        builder.Property(x => x.CarrierId).HasColumnName("carrier_id").IsRequired();
        builder.Property(x => x.ServiceType).HasColumnName("service_type").HasMaxLength(50);
        builder.Property(x => x.ShippingFee).HasColumnName("shipping_fee").HasPrecision(12, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(100);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("PENDING").IsRequired();
        builder.Property(x => x.ReceiverName).HasColumnName("receiver_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ReceiverPhone).HasColumnName("receiver_phone").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReceiverAddress).HasColumnName("receiver_address").HasMaxLength(500).IsRequired();
        builder.Property(x => x.EstimatedDeliveryAt).HasColumnName("estimated_delivery_at");
        builder.Property(x => x.DeliveredAt).HasColumnName("delivered_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(x => new { x.OrderId, x.WarehouseId }).IsUnique();
        builder.HasIndex(x => new { x.CarrierId, x.TrackingNumber }).IsUnique();

        builder.HasOne(s => s.Warehouse)
            .WithMany(w => w.Shipments)
            .HasForeignKey(s => s.WarehouseId);

        builder.HasOne(s => s.Carrier)
            .WithMany(c => c.Shipments)
            .HasForeignKey(s => s.CarrierId);
    }
}
