using System.Net;
using Npgsql;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

internal sealed record AbwabSnapshotDatabaseTarget(string Masked, bool IsLoopback);

internal static class AbwabSnapshotDatabaseTargetParser
{
    internal static AbwabSnapshotDatabaseTarget Parse(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        return new AbwabSnapshotDatabaseTarget(
            $"host={Mask(builder.Host)};port={builder.Port};database={Mask(builder.Database)}",
            IsLoopback(builder.Host));
    }

    private static bool IsLoopback(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        return host.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .All(part =>
                string.Equals(part, "localhost", StringComparison.OrdinalIgnoreCase)
                || Path.IsPathRooted(part)
                || (IPAddress.TryParse(part, out var address) && IPAddress.IsLoopback(address)));
    }

    private static string Mask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "***";
        }

        if (value.Length <= 2)
        {
            return new string('*', value.Length);
        }

        return $"{value[0]}{new string('*', Math.Min(8, value.Length - 2))}{value[^1]}";
    }
}
