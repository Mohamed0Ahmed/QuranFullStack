using System.Globalization;
using Npgsql;
using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Tafsirs;

public sealed class TafsirValidationRunner
{
    public async Task<IReadOnlyList<TafsirCheckResult>> RunPostCopyChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TafsirSourceData source,
        TafsirExpectedCounts expected,
        IReadOnlyDictionary<string, string> ayahTextsByVerseKey,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        CancellationToken ct)
    {
        var sourceCount = await TafsirCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, TafsirSql.CheckSourceCount, ct);
        var ayahMappingCount = await TafsirCommandExecutor.ExecuteScalarLongAsync(
            connection, transaction, TafsirSql.CheckAyahMappingCount, ct);

        var checks = new List<TafsirCheckResult>
        {
            new(
                TafsirInvariants.CheckPostCopySourceRows,
                TafsirImportConstants.HardSeverity,
                expected.ApprovedSources.ToString(CultureInfo.InvariantCulture),
                sourceCount.ToString(CultureInfo.InvariantCulture),
                sourceCount == expected.ApprovedSources),
            new(
                TafsirInvariants.CheckPostCopyAyahMappings,
                TafsirImportConstants.HardSeverity,
                expected.SourceAyahMappings.ToString(CultureInfo.InvariantCulture),
                ayahMappingCount.ToString(CultureInfo.InvariantCulture),
                ayahMappingCount == expected.SourceAyahMappings)
        };

        checks.AddRange(await ValidatePersistedTextAsync(connection, transaction, source, ct));
        checks.AddRange(ValidateNoQuranTextCopy(source, ayahTextsByVerseKey));

        var sourceUnchanged = await sourceUnchangedCheck(ct);
        checks.Add(new TafsirCheckResult(
            TafsirInvariants.CheckSourceUnchanged,
            TafsirImportConstants.HardSeverity,
            "local source files match manifest.json size/sha256 before and after run",
            sourceUnchanged ? "unchanged" : "changed",
            sourceUnchanged));

        return checks;
    }

    private static async Task<IReadOnlyList<TafsirCheckResult>> ValidatePersistedTextAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TafsirSourceData source,
        CancellationToken ct)
    {
        var persistedBySourceEntry = new Dictionary<(string SourceKey, string SourceEntryKey), (string Text, string Hash)>();

        await using var command = new NpgsqlCommand(TafsirSql.ReadPersistedEntryTexts, connection, transaction);
        command.CommandTimeout = TafsirCommandExecutor.CommandTimeoutSeconds;
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
                || !string.Equals(entry.TafsirText, persisted.Text, StringComparison.Ordinal)
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
            new TafsirCheckResult(
                TafsirInvariants.CheckTextUnchanged,
                TafsirImportConstants.HardSeverity,
                "stored tafsir text and hash match imported source per source",
                passed ? "exact match" : string.Join(", ", mismatches),
                passed)
        ];
    }

    private static IReadOnlyList<TafsirCheckResult> ValidateNoQuranTextCopy(
        TafsirSourceData source,
        IReadOnlyDictionary<string, string> ayahTextsByVerseKey)
    {
        var copiedKeys = source.Entries
            .Where(entry =>
                ayahTextsByVerseKey.TryGetValue(entry.SourceEntryKey, out var ayahText)
                && string.Equals(entry.TafsirText, ayahText, StringComparison.Ordinal))
            .Select(entry => $"{entry.SourceKey}:{entry.SourceEntryKey}")
            .ToList();

        return
        [
            new TafsirCheckResult(
                TafsirInvariants.CheckNoQuranTextCopy,
                TafsirImportConstants.HardSeverity,
                "no copied Quran ayah text in tafsir entries",
                copiedKeys.Count == 0 ? "none" : string.Join(", ", copiedKeys),
                copiedKeys.Count == 0)
        ];
    }
}
