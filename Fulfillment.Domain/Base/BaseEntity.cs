namespace Fulfillment.Domain.Base;

using System;

public abstract class BaseEntity<TKey> : IAuditable, ISoftDelete
{
    public TKey Id { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset? DeletedAt { get; set; }
}

public abstract class BaseEntity : BaseEntity<Guid>
{
}

public abstract class ImmutableEntity<TKey>
{
    public TKey Id { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public abstract class ImmutableEntity : ImmutableEntity<Guid>
{
}
