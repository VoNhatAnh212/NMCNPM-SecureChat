#nullable enable

namespace SecureChat.Client.Forms.Helpers;

/// <summary>
/// Provides consistent date and time formatting for the client.
/// </summary>
public static class DateTimeHelper
{
    /// <summary>Formats a date as a compact relative string.</summary>
    public static string ToRelativeString(DateTime dt)
    {
        var local = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
        var now = DateTime.Now;
        var span = now - local;

        if (span.TotalSeconds < 60)
        {
            return "just now";
        }

        if (span.TotalMinutes < 60)
        {
            var minutes = Math.Max(1, (int)Math.Floor(span.TotalMinutes));
            return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
        }

        if (span.TotalHours < 24 && local.Date == now.Date)
        {
            var hours = Math.Max(1, (int)Math.Floor(span.TotalHours));
            return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
        }

        if (local.Date == now.Date.AddDays(-1))
        {
            return "Yesterday";
        }

        return local.Year == now.Year ? local.ToString("d MMM") : local.ToString("d MMM yyyy");
    }

    /// <summary>Formats a date as display time.</summary>
    public static string ToDisplayTime(DateTime dt)
    {
        var local = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
        return local.ToString("HH:mm");
    }

    /// <summary>Formats a date as display date.</summary>
    public static string ToDisplayDate(DateTime dt)
    {
        var local = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
        return local.ToString("ddd, d MMM yyyy");
    }
}
