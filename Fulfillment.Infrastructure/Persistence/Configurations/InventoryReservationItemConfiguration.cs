namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class InventoryReservationItemConfiguration : IEntityTypeConfiguration<InventoryReservationItem>
{
    public void Configure(EntityTypeBuilder<InventoryReservationItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(ri => ri.Reservation)
            .WithMany(r => r.ReservationItems)
            .HasForeignKey(ri => ri.ReservationId);

        builder.HasOne(ri => ri.InventoryItem)
            .WithMany(i => i.ReservationItems)
            .HasForeignKey(ri => ri.InventoryItemId);
    }
}
