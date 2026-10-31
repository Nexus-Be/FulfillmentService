namespace Fulfillment.Domain.Entities;

using System;

public class SystemSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string SettingKey { get; set; }
    public required string SettingValue { get; set; }
    public required string ValueType { get; set; }
    public required string Category { get; set; }
    public string? Description { get; set; }
    public bool IsEditable { get; set; } = true;
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
