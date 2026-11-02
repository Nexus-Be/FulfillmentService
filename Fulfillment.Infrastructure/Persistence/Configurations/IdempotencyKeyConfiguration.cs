namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys");
        builder.HasKey(x => new { x.Key, x.Scope });

        builder.Property(x => x.Key).HasColumnName("key").IsRequired();
        builder.Property(x => x.Scope).HasColumnName("scope").IsRequired();
        builder.Property(x => x.RequestHash).HasColumnName("request_hash").IsRequired();
        builder.Property(x => x.ResponseStatus).HasColumnName("response_status");
        builder.Property(x => x.ResponseBody).HasColumnName("response_body").HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();

        builder.HasIndex(x => x.ExpiresAt);
    }
}
