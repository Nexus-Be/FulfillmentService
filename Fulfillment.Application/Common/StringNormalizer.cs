namespace Fulfillment.Application.Common;

using System;
using System.Linq;

public static class StringNormalizer
{
    public static string NormalizeCode(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input ?? string.Empty;
        }

        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words).ToUpperInvariant();
    }

    public static string? NormalizeTitle(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var capitalizedWords = words.Select(w =>
            char.ToUpperInvariant(w[0]) + (w.Length > 1 ? w.Substring(1).ToLowerInvariant() : string.Empty));

        return string.Join(" ", capitalizedWords);
    }
}
