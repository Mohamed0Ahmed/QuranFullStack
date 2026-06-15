using QuranDashboard.Application.Abstractions.Quran.Translations;
using QuranDashboard.Infrastructure.Files.Quran.Translations;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Translations;

public sealed class TranslationValidationRunner
{
    public async Task<IReadOnlyList<TranslationCheckResult>> RunPostCopyChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TranslationSourceData source,
        TranslationExpectedCounts expected,
        IReadOnlyDictionary<string, string> ayahTextsByVerseKey,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        CancellationToken ct)
    {
        var sourceCount = await TranslationCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, TranslationSql.CheckSourceCount, ct);
        var ayahMappingCount = await TranslationCommandExecutor.ExecuteScalarLongAsync(
            connection, transaction, TranslationSql.CheckAyahMappingCount, ct);

        var checks = new List<TranslationCheckResult>
        {
            new(
                TranslationInvariants.CheckPostCopySourceRows,
                TranslationImportConstants.HardSeverity,
                expected.ApprovedSources.ToString(CultureInfo.InvariantCulture),
                sourceCount.ToString(CultureInfo.InvariantCulture),
                sourceCount == expected.ApprovedSources),
            new(
                TranslationInvariants.CheckPostCopyAyahMappings,
                TranslationImportConstants.HardSeverity,
                expected.SourceAyahMappings.ToString(CultureInfo.InvariantCulture),
                ayahMappingCount.ToString(CultureInfo.InvariantCulture),
                ayahMappingCount == expected.SourceAyahMappings)
        };

        checks.AddRange(await ValidatePersistedTextAsync(connection, transaction, source, ct));
        checks.AddRange(ValidateNoQuranTextCopy(source, ayahTextsByVerseKey));
        checks.AddRange(await ValidatePersistedTypeCountsAsync(connection, transaction, expected, ct));
        checks.AddRange(await ValidateNoDuplicatesOrExcludedAsync(connection, transaction, source, ct));

        var sourceUnchanged = await sourceUnchangedCheck(ct);
        checks.Add(new TranslationCheckResult(
            TranslationInvariants.CheckSourceUnchanged,
            TranslationImportConstants.HardSeverity,
            "package files (manifest, display metadata, approved sources) unchanged before acceptance",
            sourceUnchanged ? "unchanged" : "changed",
            sourceUnchanged));

        return checks;
    }

    private static async Task<IReadOnlyList<TranslationCheckResult>> ValidatePersistedTextAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TranslationSourceData source,
        CancellationToken ct)
    {
        var persistedBySourceVerse = new Dictionary<(string SourceKey, string VerseKey), string>();

        await using var command = new NpgsqlCommand(TranslationSql.ReadPersistedAyahTexts, connection, transaction);
        command.CommandTimeout = TranslationCommandExecutor.CommandTimeoutSeconds;
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            persistedBySourceVerse[(reader.GetString(0), reader.GetString(1))] = reader.GetString(2);
        }

        var mismatches = new List<string>();
        foreach (var entry in source.AyahEntries)
        {
            var key = (entry.SourceKey, entry.VerseKey);
            if (!persistedBySourceVerse.TryGetValue(key, out var persistedText)
                || !string.Equals(entry.Text, persistedText, StringComparison.Ordinal))
            {
                mismatches.Add($"{entry.SourceKey}:{entry.VerseKey}");
            }
        }

        if (persistedBySourceVerse.Count != source.AyahEntries.Count)
        {
            mismatches.Add($"row-count:{persistedBySourceVerse.Count}!={source.AyahEntries.Count}");
        }

        var passed = mismatches.Count == 0;
        return
        [
            new TranslationCheckResult(
                TranslationInvariants.CheckTextUnchanged,
                TranslationImportConstants.HardSeverity,
                "stored translation text matches imported source text exactly",
                passed ? "exact match" : string.Join(", ", mismatches),
                passed)
        ];
    }

    private static IReadOnlyList<TranslationCheckResult> ValidateNoQuranTextCopy(
        TranslationSourceData source,
        IReadOnlyDictionary<string, string> ayahTextsByVerseKey)
    {
        var copiedKeys = source.AyahEntries
            .Where(entry =>
                ayahTextsByVerseKey.TryGetValue(entry.VerseKey, out var ayahText)
                && string.Equals(entry.Text, ayahText, StringComparison.Ordinal))
            .Select(entry => $"{entry.SourceKey}:{entry.VerseKey}")
            .ToList();

        return
        [
            new TranslationCheckResult(
                TranslationInvariants.CheckNoQuranTextCopy,
                TranslationImportConstants.HardSeverity,
                "no copied Quran ayah text in translation entries",
                copiedKeys.Count == 0 ? "none" : string.Join(", ", copiedKeys),
                copiedKeys.Count == 0)
        ];
    }

    private static async Task<IReadOnlyList<TranslationCheckResult>> ValidatePersistedTypeCountsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TranslationExpectedCounts expected,
        CancellationToken ct)
    {
        var simpleCount = await TranslationCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, TranslationSql.CheckSimpleSourceCount, ct);
        var withFootnotesCount = await TranslationCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, TranslationSql.CheckWithFootnotesSourceCount, ct);

        return
        [
            TranslationTypeCountValidation.BuildTypeCountsCheck(expected, simpleCount, withFootnotesCount)
        ];
    }

    private static async Task<IReadOnlyList<TranslationCheckResult>> ValidateNoDuplicatesOrExcludedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TranslationSourceData source,
        CancellationToken ct)
    {
        var duplicateCount = await CountDuplicateAyahEntriesAsync(connection, transaction, ct);
        var excludedPresent = await CountExcludedSourcesPresentAsync(connection, transaction, source, ct);

        return
        [
            new TranslationCheckResult(
                TranslationInvariants.CheckNoDuplicateAyahEntry,
                TranslationImportConstants.HardSeverity,
                "no duplicate (source_id, ayah_id) rows persisted",
                duplicateCount == 0 ? "none" : $"{duplicateCount} duplicate groups",
                duplicateCount == 0),
            new TranslationCheckResult(
                TranslationInvariants.CheckNoExcludedSources,
                TranslationImportConstants.HardSeverity,
                "no excluded source keys persisted",
                excludedPresent == 0 ? "none" : $"{excludedPresent} excluded sources found",
                excludedPresent == 0)
        ];
    }

    private static async Task<int> CountDuplicateAyahEntriesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            TranslationSql.CheckDuplicateAyahEntryCount, connection, transaction);
        command.CommandTimeout = TranslationCommandExecutor.CommandTimeoutSeconds;
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static async Task<int> CountExcludedSourcesPresentAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TranslationSourceData source,
        CancellationToken ct)
    {
        var excludedKeys = source.ExcludedSources
            .Select(excluded => excluded.SourceKey)
            .ToArray();

        if (excludedKeys.Length == 0)
        {
            return 0;
        }

        await using var command = new NpgsqlCommand(
            TranslationSql.CheckExcludedSourceKeysPresent, connection, transaction);
        command.CommandTimeout = TranslationCommandExecutor.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("excludedKeys", excludedKeys);
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }
}
