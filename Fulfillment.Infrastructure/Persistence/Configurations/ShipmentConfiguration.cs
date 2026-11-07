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
        builder.Property(x => x.ServiceType).HasColumnName("service_type").IsRequired();
        builder.Property(x => x.ShippingFee).HasColumnName("shipping_fee").HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(100);
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.ReceiverName).HasColumnName("receiver_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.ReceiverPhone).HasColumnName("receiver_phone").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReceiverAddress).HasColumnName("receiver_address").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.EstimatedDeliveryAt).HasColumnName("estimated_delivery_at");
        builder.Property(x => x.DeliveredAt).HasColumnName("delivered_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").IsRequired();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.CarrierId);

        builder.HasOne(s => s.Warehouse)
            .WithMany(w => w.Shipments)
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Carrier)
            .WithMany(c => c.Shipments)
            .HasForeignKey(s => s.CarrierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Items)
            .WithOne(i => i.Shipment)
            .HasForeignKey(i => i.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Events)
            .WithOne(e => e.Shipment)
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
