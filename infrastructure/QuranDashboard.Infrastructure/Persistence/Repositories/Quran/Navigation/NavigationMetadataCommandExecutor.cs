using System.Text.RegularExpressions;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Navigation;

internal static class NavigationMetadataCommandExecutor
{
    public const int CommandTimeoutSeconds = 600;

    private static readonly HashSet<string> NavigationTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "quran_juzs",
        "quran_hizbs",
        "quran_rubs",
        "quran_sajdas"
    };

    private static readonly HashSet<string> NavigationAyahNavColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "juz_number",
        "hizb_number",
        "rub_number"
    };

    private static readonly Regex DeleteFromTablePattern = new(
        @"\bDELETE\s+FROM\s+(?:ONLY\s+)?(?<table>[a-z_][a-z0-9_]*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TruncateTablesPattern = new(
        @"\bTRUNCATE\s+(?:TABLE\s+)?(?<tables>(?:ONLY\s+)?[a-z_][a-z0-9_]*(?:\s*,\s*(?:ONLY\s+)?[a-z_][a-z0-9_]*)*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex InsertIntoTablePattern = new(
        @"\bINSERT\s+INTO\s+(?:ONLY\s+)?(?<table>[a-z_][a-z0-9_]*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex UpdateTablePattern = new(
        @"\bUPDATE\s+(?:ONLY\s+)?(?<table>[a-z_][a-z0-9_]*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CopyTablePattern = new(
        @"\bCOPY\s+(?:ONLY\s+)?(?<table>[a-z_][a-z0-9_]*)\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CreateTempTablePattern = new(
        @"\bCREATE\s+TEMP\s+TABLE\s+(?<table>[a-z_][a-z0-9_]*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static void EnsureWriteIsolation(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        foreach (Match match in TruncateTablesPattern.Matches(sql))
        {
            foreach (var table in SplitTableList(match.Groups["tables"].Value))
            {
                EnsureNavigationTableWrite(table, "TRUNCATE");
            }
        }

        foreach (Match match in DeleteFromTablePattern.Matches(sql))
        {
            EnsureNavigationTableWrite(match.Groups["table"].Value, "DELETE");
        }

        foreach (Match match in InsertIntoTablePattern.Matches(sql))
        {
            EnsureNavigationTableWrite(match.Groups["table"].Value, "INSERT");
        }

        foreach (Match match in CopyTablePattern.Matches(sql))
        {
            EnsureCopyTargetAllowed(match.Groups["table"].Value);
        }

        foreach (Match match in UpdateTablePattern.Matches(sql))
        {
            var table = match.Groups["table"].Value;
            if (!string.Equals(table, "quran_ayahs", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Navigation import attempted to {DescribeOperation("UPDATE")} outside navigation-owned tables: {table}.");
            }

            EnsureAyahNavigationUpdateOnly(sql);
        }

        foreach (Match match in CreateTempTablePattern.Matches(sql))
        {
            EnsureTempAyahUpdateTable(match.Groups["table"].Value);
        }
    }

    public static void EnsureAllowedBinaryCopyTarget(string copyCommand) =>
        EnsureWriteIsolation(copyCommand);

    public static async Task<int> ExecuteScalarIntAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.CommandTimeout = CommandTimeoutSeconds;
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    public static async Task ExecuteNonQueryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct)
    {
        EnsureWriteIsolation(sql);

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.CommandTimeout = CommandTimeoutSeconds;
        await command.ExecuteNonQueryAsync(ct);
    }

    private static IEnumerable<string> SplitTableList(string tables) =>
        tables.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(table => table.StartsWith("ONLY ", StringComparison.OrdinalIgnoreCase)
                ? table[5..].Trim()
                : table);

    private static void EnsureNavigationTableWrite(string table, string operation)
    {
        if (!NavigationTables.Contains(table))
        {
            throw new InvalidOperationException(
                $"Navigation import attempted to {DescribeOperation(operation)} outside navigation-owned tables: {table}.");
        }
    }

    private static void EnsureCopyTargetAllowed(string table)
    {
        if (NavigationTables.Contains(table)
            || string.Equals(table, "nav_ayah_updates", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Navigation import attempted to COPY outside navigation-owned tables: {table}.");
    }

    private static void EnsureTempAyahUpdateTable(string table)
    {
        if (!string.Equals(table, "nav_ayah_updates", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Navigation import attempted to create an unexpected temp table: {table}.");
        }
    }

    private static void EnsureAyahNavigationUpdateOnly(string sql)
    {
        var setMatch = Regex.Match(sql, @"\bSET\s+", RegexOptions.IgnoreCase);
        if (!setMatch.Success)
        {
            throw new InvalidOperationException(
                "Navigation import attempted to UPDATE quran_ayahs without a navigation-column SET clause.");
        }

        var setClause = sql[(setMatch.Index + setMatch.Length)..];
        var terminator = Regex.Match(setClause, @"\s(FROM|WHERE)\s", RegexOptions.IgnoreCase);
        if (terminator.Success)
        {
            setClause = setClause[..terminator.Index];
        }

        var updatedColumns = Regex.Matches(setClause, @"(?:^|,\s*)([a-z_][a-z0-9_]*)\s*=", RegexOptions.IgnoreCase)
            .Select(match => match.Groups[1].Value)
            .ToList();

        if (updatedColumns.Count == 0)
        {
            throw new InvalidOperationException(
                "Navigation import attempted to UPDATE quran_ayahs without navigation columns.");
        }

        foreach (var column in updatedColumns)
        {
            if (!NavigationAyahNavColumns.Contains(column))
            {
                throw new InvalidOperationException(
                    $"Navigation import attempted to update a non-navigation quran_ayahs column: {column}.");
            }
        }
    }

    private static string DescribeOperation(string operation) =>
        operation.ToUpperInvariant();
}
