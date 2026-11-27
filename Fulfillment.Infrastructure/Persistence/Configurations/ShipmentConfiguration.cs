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
        builder.Property(x => x.SubOrderNo).HasColumnName("sub_order_no").HasMaxLength(100);
        builder.Property(x => x.BuyerId).HasColumnName("buyer_id");
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
        builder.Property(x => x.CarrierId).HasColumnName("carrier_id").IsRequired();
        builder.Property(x => x.ServiceType).HasColumnName("service_type").IsRequired();
        builder.Property(x => x.ShippingFee).HasColumnName("shipping_fee").HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(100);
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.ReceiverName).HasColumnName("receiver_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.ReceiverPhone).HasColumnName("receiver_phone").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReceiverAddress).HasColumnName("receiver_address").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ToProvinceName).HasColumnName("to_province_name").HasMaxLength(100);
        builder.Property(x => x.ToDistrictId).HasColumnName("to_district_id");
        builder.Property(x => x.ToWardCode).HasColumnName("to_ward_code").HasMaxLength(20);
        builder.Property(x => x.Weight).HasColumnName("weight").HasDefaultValue(500).IsRequired();
        builder.Property(x => x.Length).HasColumnName("length").HasDefaultValue(20).IsRequired();
        builder.Property(x => x.Width).HasColumnName("width").HasDefaultValue(15).IsRequired();
        builder.Property(x => x.Height).HasColumnName("height").HasDefaultValue(10).IsRequired();
        builder.Property(x => x.ConvertedWeight).HasColumnName("converted_weight");
        builder.Property(x => x.CodAmount).HasColumnName("cod_amount").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.InsuranceValue).HasColumnName("insurance_value").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.PaymentTypeId).HasColumnName("payment_type_id").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.RequiredNote).HasColumnName("required_note").IsRequired();
        builder.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);
        builder.Property(x => x.ClientOrderCode).HasColumnName("client_order_code").HasMaxLength(100);
        builder.Property(x => x.GhnOrderCode).HasColumnName("ghn_order_code").HasMaxLength(50);
        builder.Property(x => x.PrintToken).HasColumnName("print_token").HasMaxLength(200);
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
        builder.HasIndex(x => x.SubOrderNo);
        builder.HasIndex(x => x.BuyerId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.CarrierId);
        builder.HasIndex(x => x.TrackingNumber);
        builder.HasIndex(x => x.GhnOrderCode);

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
