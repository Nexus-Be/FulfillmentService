namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class ShipmentEventConfiguration : IEntityTypeConfiguration<ShipmentEvent>
{
    public void Configure(EntityTypeBuilder<ShipmentEvent> builder)
    {
        builder.ToTable("shipment_events");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.ShipmentId).HasColumnName("shipment_id").IsRequired();
        builder.Property(x => x.CarrierStatus).HasColumnName("carrier_status").HasMaxLength(100);
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(x => x.ExternalEventId).HasColumnName("external_event_id").HasMaxLength(100);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(x => x.RawPayload).HasColumnName("raw_payload").HasColumnType("jsonb");
        builder.Property(x => x.ReasonCode).HasColumnName("reason_code").HasMaxLength(50);
        builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(x => x.ShipperName).HasColumnName("shipper_name").HasMaxLength(100);
        builder.Property(x => x.ShipperPhone).HasColumnName("shipper_phone").HasMaxLength(20);
        builder.Property(x => x.PodUrl).HasColumnName("pod_url").HasMaxLength(500);
        builder.Property(x => x.WarehouseName).HasColumnName("warehouse_name").HasMaxLength(200);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(e => e.Shipment)
            .WithMany(s => s.Events)
            .HasForeignKey(e => e.ShipmentId);
    }
}
