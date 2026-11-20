namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class WebhookAuditLogConfiguration : IEntityTypeConfiguration<WebhookAuditLog>
{
    public void Configure(EntityTypeBuilder<WebhookAuditLog> builder)
    {
        builder.ToTable("webhook_audit_logs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.OrderCode).HasColumnName("order_code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ClientOrderCode).HasColumnName("client_order_code").HasMaxLength(100);
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(x => x.RawJson).HasColumnName("raw_json").HasColumnType("text");
        builder.Property(x => x.IsProcessed).HasColumnName("is_processed").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ProcessMessage).HasColumnName("process_message").HasMaxLength(500);
        builder.Property(x => x.ReceivedAt).HasColumnName("received_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.EventTimeKey).HasColumnName("event_time_key").HasMaxLength(40);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        builder.Property(x => x.NotifyOrderId).HasColumnName("notify_order_id");
        builder.Property(x => x.NotifyStatus).HasColumnName("notify_status").HasMaxLength(50);
        builder.Property(x => x.NotifyTrackingNumber).HasColumnName("notify_tracking_number").HasMaxLength(100);
        builder.Property(x => x.NotifyDescription).HasColumnName("notify_description").HasMaxLength(500);
        builder.Property(x => x.CommerceNotifiedAt).HasColumnName("commerce_notified_at");
        builder.Property(x => x.CommerceNotifyAttempts).HasColumnName("commerce_notify_attempts").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.CommerceNotifyNextRetryAt).HasColumnName("commerce_notify_next_retry_at");
        builder.HasIndex(x => new { x.CommerceNotifiedAt, x.CommerceNotifyNextRetryAt }).HasDatabaseName("idx_webhook_audit_logs_notify_pending");

        builder.HasIndex(x => x.OrderCode);
        builder.HasIndex(x => new { x.OrderCode, x.Status, x.EventTimeKey }).HasDatabaseName("idx_webhook_audit_logs_event_key");
        builder.HasIndex(x => x.ReceivedAt);
    }
}

