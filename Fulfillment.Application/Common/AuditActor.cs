namespace Fulfillment.Application.Common;

using System.Threading;

/// <summary>The user behind the current request, so changes saved deep in the data layer can be attributed to them.</summary>
public static class AuditActor
{
    private static readonly AsyncLocal<string?> Current = new();

    public static string? UserId
    {
        get => Current.Value;
        set => Current.Value = value;
    }
}
