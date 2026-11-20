namespace Fulfillment.Infrastructure.Carriers.Ghn;

using System;
using System.Globalization;
using System.Text.Json;

/// <summary>
/// The webhook "Time" is an untyped value (ISO text, epoch seconds or epoch milliseconds). This turns it into one comparable key so a repeated
/// delivery of the same event is recognised and a second failed attempt (different time) is not mistaken for the first.
/// Returns null when there is no usable time, and the caller then falls back to the order code and status alone.
/// </summary>
public static class GhnEventTime
{
    public static string? Key(object? time)
    {
        var parsed = time switch
        {
            null => (DateTimeOffset?)null,
            JsonElement element => FromJson(element),
            DateTimeOffset offset => offset,
            DateTime dateTime => new DateTimeOffset(dateTime.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) : dateTime.ToUniversalTime()),
            string text => FromString(text),
            long number => FromEpoch(number),
            int number => FromEpoch(number),
            double number => FromEpoch((long)number),
            _ => FromString(Convert.ToString(time, CultureInfo.InvariantCulture))
        };

        return parsed?.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset? FromJson(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => FromString(element.GetString()),
        JsonValueKind.Number when element.TryGetInt64(out var number) => FromEpoch(number),
        JsonValueKind.Number => FromEpoch((long)element.GetDouble()),
        _ => null
    };

    private static DateTimeOffset? FromString(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = text.Trim();
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            return FromEpoch(number);
        }

        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;
    }

    private static DateTimeOffset? FromEpoch(long value)
    {
        if (value <= 0)
        {
            return null;
        }

        // 10^11 seconds is year 5138, so anything larger is milliseconds.
        return value > 100_000_000_000L ? DateTimeOffset.FromUnixTimeMilliseconds(value) : DateTimeOffset.FromUnixTimeSeconds(value);
    }
}
