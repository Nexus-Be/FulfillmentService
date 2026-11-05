namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class CarrierConfiguration : IEntityTypeConfiguration<Carrier>
{
    public void Configure(EntityTypeBuilder<Carrier> builder)
    {
        builder.ToTable("Carriers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Priority).HasDefaultValue(0).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasMany(c => c.Shipments)
            .WithOne(s => s.Carrier)
            .HasForeignKey(s => s.CarrierId);
    }
}
