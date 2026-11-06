namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class CarrierConfiguration : IEntityTypeConfiguration<Carrier>
{
    public void Configure(EntityTypeBuilder<Carrier> builder)
    {
        builder.ToTable("carriers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Priority).HasColumnName("priority").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasMany(c => c.Shipments)
            .WithOne(s => s.Carrier)
            .HasForeignKey(s => s.CarrierId);
    }
}
