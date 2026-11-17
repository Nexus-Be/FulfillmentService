namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class InventoryReservationItemConfiguration : IEntityTypeConfiguration<InventoryReservationItem>
{
    public void Configure(EntityTypeBuilder<InventoryReservationItem> builder)
    {
        builder.ToTable("inventory_reservation_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.ReservationId).HasColumnName("reservation_id").IsRequired();
        builder.Property(x => x.InventoryItemId).HasColumnName("inventory_item_id").IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.ReservationId, x.InventoryItemId }).IsUnique();

        builder.HasOne(x => x.Reservation)
            .WithMany(r => r.ReservationItems)
            .HasForeignKey(x => x.ReservationId);

        builder.HasOne(x => x.InventoryItem)
            .WithMany(i => i.ReservationItems)
            .HasForeignKey(x => x.InventoryItemId);
    }
}
