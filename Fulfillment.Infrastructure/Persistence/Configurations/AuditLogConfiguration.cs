namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.ServiceName).HasColumnName("service_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.TraceId).HasColumnName("trace_id").HasMaxLength(100);
        builder.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(100);
        builder.Property(x => x.UserName).HasColumnName("user_name").HasMaxLength(255);
        builder.Property(x => x.Action).HasColumnName("action").IsRequired();
        builder.Property(x => x.EntityName).HasColumnName("entity_name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(150).IsRequired();
        builder.Property(x => x.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        builder.Property(x => x.NewValues).HasColumnName("new_values").HasColumnType("jsonb");
        builder.Property(x => x.ChangedColumns).HasColumnName("changed_columns").HasColumnType("jsonb");
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.IsSynced).HasColumnName("is_synced").HasDefaultValue(false).IsRequired();
    }
}
