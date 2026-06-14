using NpgsqlTypes;
using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Tafsirs;

internal static class TafsirBulkCopier
{
    public static async Task CopySourceFilesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TafsirSourceData source,
        DateTimeOffset importedAtUtc,
        CancellationToken ct)
    {
        foreach (var sourceDto in source.Sources)
        {
            var sourceEntries = source.Entries
                .Where(entry => entry.SourceKey == sourceDto.SourceKey)
                .ToList();
            var sourceAyahEntries = source.AyahEntries
                .Where(entry => entry.SourceKey == sourceDto.SourceKey)
                .ToList();

            await CopySourceFileAsync(
                connection,
                transaction,
                sourceDto,
                sourceEntries,
                sourceAyahEntries,
                importedAtUtc,
                ct);
        }
    }

    public static async Task CopySourceFileAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TafsirSourceDto source,
        IReadOnlyList<TafsirEntryDto> entries,
        IReadOnlyList<TafsirAyahEntryDto> ayahEntries,
        DateTimeOffset importedAtUtc,
        CancellationToken ct)
    {
        await CopySingleSourceRowAsync(connection, source, importedAtUtc, ct);

        var sourceId = await ReadSourceIdBySourceKeyAsync(
            connection, transaction, source.SourceKey, ct);

        if (entries.Count == 0)
        {
            return;
        }

        await CopyEntryRowsAsync(connection, sourceId, entries, ct);

        var entryIdsBySourceEntryKey = await ReadEntryIdsForSourceAsync(
            connection, transaction, sourceId, source.SourceKey, ct);

        await CopyAyahEntryRowsAsync(
            connection,
            sourceId,
            source.SourceKey,
            ayahEntries,
            entryIdsBySourceEntryKey,
            ct);
    }

    private static async Task CopySingleSourceRowAsync(
        NpgsqlConnection connection,
        TafsirSourceDto row,
        DateTimeOffset importedAtUtc,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_tafsir_sources (
                source_key,
                language_code,
                language_name_ar,
                language_name_en,
                direction,
                display_name_ar,
                short_name_ar,
                display_name_en,
                short_name_en,
                contributor_key,
                contributor_name_ar,
                contributor_name_en,
                contributor_type,
                resource_kind,
                tafsir_kind,
                content_coverage_count,
                package_file,
                source_file_original,
                sha256,
                file_size_bytes,
                license_status,
                provenance_status,
                manifest_metadata,
                imported_at_utc)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);
        await importer.StartRowAsync(ct);
        await importer.WriteAsync(row.SourceKey, ct);
        await importer.WriteAsync(row.LanguageCode, ct);
        await importer.WriteAsync(row.LanguageNameAr, ct);
        await importer.WriteAsync(row.LanguageNameEn, ct);
        await importer.WriteAsync(row.Direction, ct);
        await importer.WriteAsync(row.DisplayNameAr, ct);
        await importer.WriteAsync(row.ShortNameAr, ct);
        await importer.WriteAsync(row.DisplayNameEn, ct);
        await importer.WriteAsync(row.ShortNameEn, ct);
        await importer.WriteAsync(row.ContributorKey, NpgsqlDbType.Text, ct);
        await importer.WriteAsync(row.ContributorNameAr, NpgsqlDbType.Text, ct);
        await importer.WriteAsync(row.ContributorNameEn, NpgsqlDbType.Text, ct);
        await importer.WriteAsync(row.ContributorType, ct);
        await importer.WriteAsync(row.ResourceKind, ct);
        await importer.WriteAsync(row.TafsirKind, ct);
        await importer.WriteAsync(row.ContentCoverageCount, ct);
        await importer.WriteAsync(row.PackageFile, ct);
        await importer.WriteAsync(row.SourceFileOriginal, ct);
        await importer.WriteAsync(row.Sha256, ct);
        await importer.WriteAsync(row.FileSizeBytes, ct);
        await importer.WriteAsync(row.LicenseStatus, ct);
        await importer.WriteAsync(row.ProvenanceStatus, ct);
        await importer.WriteAsync(row.ManifestMetadataJson, NpgsqlDbType.Jsonb, ct);
        await importer.WriteAsync(importedAtUtc.UtcDateTime, NpgsqlDbType.TimestampTz, ct);
        await importer.CompleteAsync(ct);
    }

    private static async Task CopyEntryRowsAsync(
        NpgsqlConnection connection,
        int sourceId,
        IReadOnlyList<TafsirEntryDto> entries,
        CancellationToken ct)
    {
        if (entries.Count == 0)
        {
            return;
        }

        const string copyCommand = """
            COPY quran_tafsir_entries (
                source_id,
                source_entry_key,
                leader_ayah_id,
                tafsir_text,
                covered_ayah_count,
                covered_ayah_keys,
                source_shape,
                text_hash)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var row in entries)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(sourceId, ct);
            await importer.WriteAsync(row.SourceEntryKey, ct);
            await importer.WriteAsync(row.LeaderAyahId, ct);
            await importer.WriteAsync(row.TafsirText, ct);
            await importer.WriteAsync(row.CoveredAyahCount, ct);
            await importer.WriteAsync(row.CoveredAyahKeysJson, NpgsqlDbType.Jsonb, ct);
            await importer.WriteAsync(row.SourceShape, ct);
            await importer.WriteAsync(row.TextHash, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopyAyahEntryRowsAsync(
        NpgsqlConnection connection,
        int sourceId,
        string sourceKey,
        IReadOnlyList<TafsirAyahEntryDto> ayahEntries,
        IReadOnlyDictionary<string, long> entryIdsBySourceEntryKey,
        CancellationToken ct)
    {
        if (ayahEntries.Count == 0)
        {
            return;
        }

        const string copyCommand = """
            COPY quran_tafsir_ayah_entries (
                source_id,
                ayah_id,
                tafsir_entry_id,
                verse_key,
                source_value_kind,
                source_leader_verse_key,
                is_group_leader,
                sort_order)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var row in ayahEntries)
        {
            var entryKey = row.IsGroupLeader || row.SourceValueKind == TafsirImportConstants.ValueKindFlat
                ? row.VerseKey
                : row.SourceLeaderVerseKey;

            if (!entryIdsBySourceEntryKey.TryGetValue(entryKey, out var entryId))
            {
                throw new InvalidDataException(
                    $"Tafsir entry id for source '{sourceKey}' and key '{entryKey}' was not found after COPY.");
            }

            await importer.StartRowAsync(ct);
            await importer.WriteAsync(sourceId, ct);
            await importer.WriteAsync(row.AyahId, ct);
            await importer.WriteAsync(entryId, ct);
            await importer.WriteAsync(row.VerseKey, ct);
            await importer.WriteAsync(row.SourceValueKind, ct);
            await importer.WriteAsync(row.SourceLeaderVerseKey, ct);
            await importer.WriteAsync(row.IsGroupLeader, ct);
            await importer.WriteAsync(row.SortOrder, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task<int> ReadSourceIdBySourceKeyAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sourceKey,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(TafsirSql.ReadSourceIdBySourceKey, connection, transaction);
        command.Parameters.AddWithValue("sourceKey", sourceKey);
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    private static async Task<Dictionary<string, long>> ReadEntryIdsForSourceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int sourceId,
        string sourceKey,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(TafsirSql.ReadEntryIdsForSource, connection, transaction);
        command.Parameters.AddWithValue("sourceId", sourceId);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var result = new Dictionary<string, long>(StringComparer.Ordinal);
        while (await reader.ReadAsync(ct))
        {
            result[reader.GetString(0)] = reader.GetInt64(1);
        }

        if (result.Count == 0)
        {
            throw new InvalidDataException($"No tafsir entries were found for source '{sourceKey}' after COPY.");
        }

        return result;
    }
}
