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
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(x => x.ExternalEventId).HasColumnName("external_event_id").HasMaxLength(100);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(x => x.RawPayload).HasColumnName("raw_payload").HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(x => new { x.ShipmentId, x.ExternalEventId }).IsUnique();
        builder.HasIndex(x => new { x.ShipmentId, x.OccurredAt });

        builder.HasOne(e => e.Shipment)
            .WithMany(s => s.Events)
            .HasForeignKey(e => e.ShipmentId);
    }
}
