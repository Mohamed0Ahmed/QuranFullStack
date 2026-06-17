using QuranDashboard.Application.Abstractions.Quran.FullI3rab;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.FullI3rab;

public sealed class FullI3rabValidationRunner
{
    public async Task<IReadOnlyList<FullI3rabCheckResult>> RunPostCopyChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FullI3rabSourceData source,
        FullI3rabExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sourceUnchangedCheck);

        var sourceCount = await FullI3rabCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, FullI3rabSql.CheckSourceCount, ct);
        var ayahMappingCount = await FullI3rabCommandExecutor.ExecuteScalarLongAsync(
            connection, transaction, FullI3rabSql.CheckAyahMappingCount, ct);
        var coverageSum = await FullI3rabCommandExecutor.ExecuteScalarLongAsync(
            connection, transaction, FullI3rabSql.CheckCoverageSum, ct);
        var distinctAyahs = await FullI3rabCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, FullI3rabSql.CheckDistinctAyahCount, ct);
        var entrySourceMismatches = await FullI3rabCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, FullI3rabSql.CheckEntrySourceMismatch, ct);
        var emptyHtmlRows = await FullI3rabCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, FullI3rabSql.CheckEmptyHtml, ct);
        var unresolvedJunctionAyahs = await FullI3rabCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, FullI3rabSql.CheckUnresolvedJunctionAyahIds, ct);
        var unresolvedLeaderAyahs = await FullI3rabCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, FullI3rabSql.CheckUnresolvedLeaderAyahIds, ct);
        var perSourceMappingViolations = await ExecutePerSourceViolationCountAsync(
            connection, transaction, FullI3rabSql.CheckPerSourceAyahMappingViolations, expected.AyahsPerSource, ct);
        var perSourceCoverageViolations = await ExecutePerSourceViolationCountAsync(
            connection, transaction, FullI3rabSql.CheckPerSourceCoverageViolations, expected.AyahsPerSource, ct);

        var expectedMappings = expected.AyahMappingTotal;
        var ayahsPerSourceText = expected.AyahsPerSource.ToString(CultureInfo.InvariantCulture);
        var sourcesText = expected.Sources.ToString(CultureInfo.InvariantCulture);
        var expectedMappingsText = expectedMappings.ToString(CultureInfo.InvariantCulture);
        var ayahMappingsPassed = ayahMappingCount == expectedMappings && perSourceMappingViolations == 0;
        var coverageSumPassed = coverageSum == expectedMappings && perSourceCoverageViolations == 0;

        var checks = new List<FullI3rabCheckResult>
        {
            new(
                FullI3rabInvariants.CheckPostCopySourceRows,
                FullI3rabImportConstants.HardSeverity,
                sourcesText,
                sourceCount.ToString(CultureInfo.InvariantCulture),
                sourceCount == expected.Sources),
            new(
                FullI3rabInvariants.CheckPostCopyAyahMappings,
                FullI3rabImportConstants.HardSeverity,
                expectedMappingsText,
                FormatTotalWithPerSourceViolations(ayahMappingCount, perSourceMappingViolations),
                ayahMappingsPassed),
            new(
                FullI3rabInvariants.CheckPostCopyCoverageSum,
                FullI3rabImportConstants.HardSeverity,
                expectedMappingsText,
                FormatTotalWithPerSourceViolations(coverageSum, perSourceCoverageViolations),
                coverageSumPassed),
            new(
                FullI3rabInvariants.CheckPostCopyEntrySource,
                FullI3rabImportConstants.HardSeverity,
                "0 junction rows with entry source mismatch",
                entrySourceMismatches == 0
                    ? "0"
                    : entrySourceMismatches.ToString(CultureInfo.InvariantCulture),
                entrySourceMismatches == 0),
            new(
                FullI3rabInvariants.CheckPostCopyAyahResolved,
                FullI3rabImportConstants.HardSeverity,
                "all junction and leader ayah ids resolve in quran_ayahs",
                unresolvedJunctionAyahs == 0 && unresolvedLeaderAyahs == 0
                    ? "all resolved"
                    : $"junction={unresolvedJunctionAyahs}, leader={unresolvedLeaderAyahs}",
                unresolvedJunctionAyahs == 0 && unresolvedLeaderAyahs == 0),
            new(
                FullI3rabInvariants.CheckPostCopyNoEmptyHtml,
                FullI3rabImportConstants.HardSeverity,
                "0 empty i3rab_html rows",
                emptyHtmlRows == 0
                    ? "0"
                    : emptyHtmlRows.ToString(CultureInfo.InvariantCulture),
                emptyHtmlRows == 0)
        };

        checks.AddRange(await ValidatePersistedHtmlAsync(connection, transaction, source, ct));

        var sourceUnchanged = await sourceUnchangedCheck(ct);
        checks.Add(new FullI3rabCheckResult(
            FullI3rabInvariants.CheckPostCopySourceUnchanged,
            FullI3rabImportConstants.HardSeverity,
            "local source files match manifest.json size/sha256 before and after run",
            sourceUnchanged ? "unchanged" : "changed",
            sourceUnchanged));

        checks.Add(new FullI3rabCheckResult(
            FullI3rabInvariants.CheckPostCopyDistinctAyahs,
            FullI3rabImportConstants.InfoSeverity,
            ayahsPerSourceText,
            distinctAyahs.ToString(CultureInfo.InvariantCulture),
            distinctAyahs == expected.AyahsPerSource));

        return checks;
    }

    private static string FormatTotalWithPerSourceViolations(long total, int perSourceViolations) =>
        perSourceViolations == 0
            ? total.ToString(CultureInfo.InvariantCulture)
            : $"total={total}, perSourceViolations={perSourceViolations.ToString(CultureInfo.InvariantCulture)}";

    private static async Task<int> ExecutePerSourceViolationCountAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        int expectedPerSource,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.CommandTimeout = FullI3rabCommandExecutor.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("expectedPerSource", expectedPerSource);
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static async Task<IReadOnlyList<FullI3rabCheckResult>> ValidatePersistedHtmlAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FullI3rabSourceData source,
        CancellationToken ct)
    {
        var persistedBySourceEntry = new Dictionary<(string SourceKey, string SourceEntryKey), (string Html, string Hash)>();

        await using var command = new NpgsqlCommand(FullI3rabSql.ReadPersistedEntryHtml, connection, transaction);
        command.CommandTimeout = FullI3rabCommandExecutor.CommandTimeoutSeconds;
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            persistedBySourceEntry[(reader.GetString(0), reader.GetString(1))] =
                (reader.GetString(2), reader.GetString(3));
        }

        var mismatches = new List<string>();
        foreach (var entry in source.Entries)
        {
            var key = (entry.SourceKey, entry.SourceEntryKey);
            if (!persistedBySourceEntry.TryGetValue(key, out var persisted)
                || !string.Equals(entry.I3rabHtml, persisted.Html, StringComparison.Ordinal)
                || !string.Equals(entry.TextHash, persisted.Hash, StringComparison.Ordinal))
            {
                mismatches.Add($"{entry.SourceKey}:{entry.SourceEntryKey}");
            }
        }

        if (persistedBySourceEntry.Count != source.Entries.Count)
        {
            mismatches.Add($"row-count:{persistedBySourceEntry.Count}!={source.Entries.Count}");
        }

        var passed = mismatches.Count == 0;
        return
        [
            new FullI3rabCheckResult(
                FullI3rabInvariants.CheckPostCopyHtmlUnchanged,
                FullI3rabImportConstants.HardSeverity,
                "stored i3rab_html and hash match imported source per source",
                passed ? "exact match" : string.Join(", ", mismatches),
                passed)
        ];
    }
}
