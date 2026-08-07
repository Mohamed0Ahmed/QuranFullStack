using System.Globalization;
using System.Text;

namespace QuranDashboard.Application.Abstractions.Access;

public static class AccessAuditCursorCodec
{
    public static string Encode(DateTimeOffset occurredAtUtc, long id)
    {
        var value = string.Concat(
            occurredAtUtc.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture),
            ":",
            id.ToString(CultureInfo.InvariantCulture));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TryDecode(string? cursor, out DateTimeOffset occurredAtUtc, out long id)
    {
        occurredAtUtc = default;
        id = default;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return cursor is null;
        }

        try
        {
            var padding = new string('=', (4 - cursor.Length % 4) % 4);
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(
                cursor.Replace('-', '+').Replace('_', '/') + padding));
            var parts = value.Split(':', StringSplitOptions.None);
            if (parts.Length != 2
                || !long.TryParse(parts[0], CultureInfo.InvariantCulture, out var utcTicks)
                || !long.TryParse(parts[1], CultureInfo.InvariantCulture, out id)
                || id < 1)
            {
                return false;
            }

            occurredAtUtc = new DateTimeOffset(utcTicks, TimeSpan.Zero);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
