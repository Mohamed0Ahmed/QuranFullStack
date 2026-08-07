namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal static class ExternalReadOnlyDatabaseOptIn
{
    internal const string ModeVariable = "QURAN_DASHBOARD_TEST_EXTERNAL_DB_MODE";
    internal const string AcknowledgedMode = "READ_ONLY_ACKNOWLEDGED";

    internal const string MushafReaderConnectionVariable = "MUSHAF_READER_REAL_DB_CONNECTION";
    internal const string UniqueWordsConnectionVariable = "UNIQUE_WORDS_REAL_DB_CONNECTION";
    internal const string RootsExplorerConnectionVariable = "ROOTS_EXPLORER_REAL_DB_CONNECTION";
    internal const string MorphologyExplorersConnectionVariable = "MORPHOLOGY_EXPLORERS_REAL_DB_CONNECTION";
    internal const string WordTypesConnectionVariable = "WORD_TYPES_REAL_DB_CONNECTION";

    internal static readonly IReadOnlyList<string> ConnectionVariables =
    [
        MushafReaderConnectionVariable,
        UniqueWordsConnectionVariable,
        RootsExplorerConnectionVariable,
        MorphologyExplorersConnectionVariable,
        WordTypesConnectionVariable
    ];

    internal static PostgreSqlDatabaseLease? TryLease(string connectionVariable)
    {
        var connectionString = ResolveConnectionString(connectionVariable, Environment.GetEnvironmentVariable);

        return connectionString is null
            ? null
            : PostgreSqlTestProcess.UseExternalReadOnlyDatabase(connectionString);
    }

    internal static string? ResolveConnectionString(
        string connectionVariable,
        Func<string, string?> readVariable)
    {
        if (!ConnectionVariables.Contains(connectionVariable))
        {
            throw new ArgumentException(
                $"'{connectionVariable}' is not one of the feature read-only database overrides: "
                + $"{string.Join(", ", ConnectionVariables)}.",
                nameof(connectionVariable));
        }

        var connectionString = readVariable(connectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        if (!string.Equals(readVariable(ModeVariable), AcknowledgedMode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"'{connectionVariable}' points at a database this test process does not own, so it is refused. "
                + $"It is honoured only outside the normal gates, and only when "
                + $"'{ModeVariable}={AcknowledgedMode}' accompanies it. Normal gates unset it.");
        }

        var companions = ConnectionVariables
            .Where(variable => variable != connectionVariable)
            .Where(variable => !string.IsNullOrWhiteSpace(readVariable(variable)))
            .ToArray();

        if (companions.Length > 0)
        {
            throw new InvalidOperationException(
                $"'{ModeVariable}={AcknowledgedMode}' acknowledges exactly one external read-only database, but "
                + $"'{connectionVariable}' is set alongside '{string.Join("', '", companions)}'.");
        }

        return connectionString;
    }
}
