using QuranDashboard.Application.Abstractions.Quran.FullI3rab;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.FullI3rab;

internal static class FullI3rabBulkCopier
{
    public static async Task CopyAllSourcesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FullI3rabSourceData source,
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

            await CopySingleSourceAsync(
                connection,
                transaction,
                sourceDto,
                sourceEntries,
                sourceAyahEntries,
                importedAtUtc,
                ct);
        }
    }

    private static async Task CopySingleSourceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FullI3rabSourceDto source,
        IReadOnlyList<FullI3rabEntryDto> entries,
        IReadOnlyList<FullI3rabAyahEntryDto> ayahEntries,
        DateTimeOffset importedAtUtc,
        CancellationToken ct)
    {
        await CopySourceRowAsync(connection, source, importedAtUtc, ct);

        var sourceId = await ReadSourceIdBySourceKeyAsync(
            connection, transaction, source.SourceKey, ct);

        if (entries.Count == 0)
        {
            return;
        }

        await CopyEntryRowsAsync(connection, transaction, sourceId, entries, ct);

        var entryIdsBySourceEntryKey = await ReadEntryIdsForSourceAsync(
            connection, transaction, sourceId, source.SourceKey, ct);

        await CopyAyahEntryRowsAsync(
            connection,
            transaction,
            sourceId,
            source.SourceKey,
            ayahEntries,
            entryIdsBySourceEntryKey,
            ct);
    }

    private static async Task CopySourceRowAsync(
        NpgsqlConnection connection,
        FullI3rabSourceDto row,
        DateTimeOffset importedAtUtc,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_full_i3rab_sources (
                source_key,
                display_name_ar,
                short_name_ar,
                display_name_en,
                short_name_en,
                language_code,
                direction,
                contributor_name_ar,
                contributor_name_en,
                resource_kind,
                markup_format,
                has_quran_quotation_markup,
                content_coverage_count,
                package_file,
                source_file_original,
                sha256,
                file_size_bytes,
                license_status,
                provenance_status,
                usage_scope,
                manifest_metadata,
                imported_at_utc)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);
        await importer.StartRowAsync(ct);
        await importer.WriteAsync(row.SourceKey, ct);
        await importer.WriteAsync(row.DisplayNameAr, ct);
        await importer.WriteAsync(row.ShortNameAr, ct);
        await importer.WriteAsync(row.DisplayNameEn, ct);
        await importer.WriteAsync(row.ShortNameEn, ct);
        await importer.WriteAsync(row.LanguageCode, ct);
        await importer.WriteAsync(row.Direction, ct);
        await importer.WriteAsync(row.ContributorNameAr, NpgsqlDbType.Text, ct);
        await importer.WriteAsync(row.ContributorNameEn, NpgsqlDbType.Text, ct);
        await importer.WriteAsync(row.ResourceKind, ct);
        await importer.WriteAsync(row.MarkupFormat, ct);
        await importer.WriteAsync(row.HasQuranQuotationMarkup, ct);
        await importer.WriteAsync(row.ContentCoverageCount, ct);
        await importer.WriteAsync(row.PackageFile, ct);
        await importer.WriteAsync(row.SourceFileOriginal, ct);
        await importer.WriteAsync(row.Sha256, ct);
        await importer.WriteAsync(row.FileSizeBytes, ct);
        await importer.WriteAsync(row.LicenseStatus, ct);
        await importer.WriteAsync(row.ProvenanceStatus, ct);
        await importer.WriteAsync(row.UsageScope, ct);
        await importer.WriteAsync(row.ManifestMetadataJson, NpgsqlDbType.Jsonb, ct);
        await importer.WriteAsync(importedAtUtc.UtcDateTime, NpgsqlDbType.TimestampTz, ct);
        await importer.CompleteAsync(ct);
    }

    private static async Task CopyEntryRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int sourceId,
        IReadOnlyList<FullI3rabEntryDto> entries,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_full_i3rab_entries (
                source_id,
                source_entry_key,
                leader_ayah_id,
                i3rab_html,
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
            await importer.WriteAsync(row.I3rabHtml, ct);
            await importer.WriteAsync(row.CoveredAyahCount, ct);
            await importer.WriteAsync(row.CoveredAyahKeysJson, NpgsqlDbType.Jsonb, ct);
            await importer.WriteAsync(row.SourceShape, ct);
            await importer.WriteAsync(row.TextHash, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopyAyahEntryRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int sourceId,
        string sourceKey,
        IReadOnlyList<FullI3rabAyahEntryDto> ayahEntries,
        IReadOnlyDictionary<string, long> entryIdsBySourceEntryKey,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_full_i3rab_ayah_entries (
                source_id,
                ayah_id,
                entry_id,
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
            var entryKey = row.IsGroupLeader || row.SourceValueKind == FullI3rabImportConstants.ValueKindFlat
                ? row.VerseKey
                : row.SourceLeaderVerseKey;

            if (!entryIdsBySourceEntryKey.TryGetValue(entryKey, out var entryId))
            {
                throw new InvalidDataException(
                    $"Full i'rab entry id for source '{sourceKey}' and key '{entryKey}' was not found after COPY.");
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
        await using var command = new NpgsqlCommand(FullI3rabSql.ReadSourceIdBySourceKey, connection, transaction);
        command.Parameters.AddWithValue("sourceKey", sourceKey);
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static async Task<Dictionary<string, long>> ReadEntryIdsForSourceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int sourceId,
        string sourceKey,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(FullI3rabSql.ReadEntryIdsForSource, connection, transaction);
        command.Parameters.AddWithValue("sourceId", sourceId);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var result = new Dictionary<string, long>(StringComparer.Ordinal);
        while (await reader.ReadAsync(ct))
        {
            result[reader.GetString(0)] = reader.GetInt64(1);
        }

        if (result.Count == 0)
        {
            throw new InvalidDataException($"No full i'rab entries were found for source '{sourceKey}' after COPY.");
        }

        return result;
    }
}
