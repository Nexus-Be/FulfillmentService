namespace Fulfillment.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fulfillment.Domain.Entities;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.SettingKey).HasColumnName("setting_key").IsRequired();
        builder.Property(x => x.SettingValue).HasColumnName("setting_value").IsRequired();
        builder.Property(x => x.ValueType).HasColumnName("value_type").IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.IsEditable).HasColumnName("is_editable").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(x => x.SettingKey).IsUnique();
    }
}
