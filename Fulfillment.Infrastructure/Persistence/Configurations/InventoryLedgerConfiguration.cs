namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class InventoryLedgerConfiguration : IEntityTypeConfiguration<InventoryLedger>
{
    public void Configure(EntityTypeBuilder<InventoryLedger> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReferenceId).HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasOne(l => l.InventoryItem)
            .WithMany(i => i.Ledgers)
            .HasForeignKey(l => l.InventoryItemId);

        builder.HasOne(l => l.Reservation)
            .WithMany(r => r.Ledgers)
            .HasForeignKey(l => l.ReservationId);
    }
}
