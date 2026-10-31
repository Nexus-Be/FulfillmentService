namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(x => new { x.MessageId, x.Consumer });

        builder.Property(x => x.MessageId).HasColumnName("message_id");
        builder.Property(x => x.Consumer).HasColumnName("consumer").IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").IsRequired();
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ReceivedAt).HasColumnName("received_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue("PENDING").IsRequired();
        builder.Property(x => x.RetryCount).HasColumnName("retry_count").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.LastError).HasColumnName("last_error");

        builder.HasIndex(x => new { x.Status, x.ReceivedAt });
    }
}
