namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AggregateType).HasColumnName("aggregate_type").IsRequired();
        builder.Property(x => x.AggregateId).HasColumnName("aggregate_id").HasMaxLength(100).IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").IsRequired();
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Headers).HasColumnName("headers").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.PublishedAt).HasColumnName("published_at");
        builder.Property(x => x.RetryCount).HasColumnName("retry_count").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.LastError).HasColumnName("last_error");
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue("PENDING").IsRequired();

        builder.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
