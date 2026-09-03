namespace QuranDashboard.TestRuntime;

internal static class PostgreSqlIdentifier
{
    internal static string Quote(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
