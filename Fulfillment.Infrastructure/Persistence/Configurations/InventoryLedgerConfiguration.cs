namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class InventoryLedgerConfiguration : IEntityTypeConfiguration<InventoryLedger>
{
    public void Configure(EntityTypeBuilder<InventoryLedger> builder)
    {
        builder.ToTable("inventory_ledger");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.InventoryItemId).HasColumnName("inventory_item_id").IsRequired();
        builder.Property(x => x.MovementType).HasColumnName("movement_type").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(x => x.ReservationId).HasColumnName("reservation_id");
        builder.Property(x => x.ReferenceType).HasColumnName("reference_type").HasMaxLength(30);
        builder.Property(x => x.ReferenceId).HasColumnName("reference_id").HasMaxLength(100);
        builder.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(x => new { x.InventoryItemId, x.CreatedAt });
        builder.HasIndex(x => x.ReservationId);

        builder.HasOne(x => x.InventoryItem)
            .WithMany(i => i.Ledgers)
            .HasForeignKey(x => x.InventoryItemId);
    }
}
