namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class ShipmentEventConfiguration : IEntityTypeConfiguration<ShipmentEvent>
{
    public void Configure(EntityTypeBuilder<ShipmentEvent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CarrierStatus).HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.ExternalEventId).HasMaxLength(100);
        builder.Property(x => x.RawPayload).HasColumnType("jsonb");

        builder.HasOne(e => e.Shipment)
            .WithMany(s => s.Events)
            .HasForeignKey(e => e.ShipmentId);
    }
}
