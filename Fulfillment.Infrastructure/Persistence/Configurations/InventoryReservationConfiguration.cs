namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReferenceType).HasConversion<string>();
        builder.Property(x => x.ReferenceId).HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<string>();

        builder.HasMany(r => r.ReservationItems)
            .WithOne(ri => ri.Reservation)
            .HasForeignKey(ri => ri.ReservationId);

        builder.HasMany(r => r.Ledgers)
            .WithOne(l => l.Reservation)
            .HasForeignKey(l => l.ReservationId);
    }
}
