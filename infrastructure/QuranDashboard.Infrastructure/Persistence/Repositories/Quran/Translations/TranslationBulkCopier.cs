using NpgsqlTypes;
using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Translations;

internal static class TranslationBulkCopier
{
    public static async Task CopySourceFilesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TranslationSourceData source,
        CancellationToken ct)
    {
        foreach (var sourceDto in source.Sources)
        {
            var sourceAyahEntries = source.AyahEntries
                .Where(entry => entry.SourceKey == sourceDto.SourceKey)
                .ToList();

            await CopySourceFileAsync(
                connection,
                transaction,
                sourceDto,
                sourceAyahEntries,
                ct);
        }
    }

    public static async Task CopySourceFileAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TranslationSourceDto source,
        IReadOnlyList<TranslationAyahEntryDto> ayahEntries,
        CancellationToken ct)
    {
        await CopySingleSourceRowAsync(connection, source, ct);

        var sourceId = await ReadSourceIdBySourceKeyAsync(
            connection, transaction, source.SourceKey, ct);

        if (ayahEntries.Count == 0)
        {
            return;
        }

        await CopyAyahEntryRowsAsync(connection, sourceId, ayahEntries, ct);
    }

    private static async Task CopySingleSourceRowAsync(
        NpgsqlConnection connection,
        TranslationSourceDto row,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_translation_sources (
                source_key,
                language_code,
                language_name_en,
                language_name_ar,
                native_name,
                direction,
                translation_type,
                display_name_en,
                display_name_ar,
                translator_key,
                translator_name_en,
                translator_name_ar,
                contains_inline_footnotes,
                contains_html_markup,
                content_coverage_count)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);
        await importer.StartRowAsync(ct);
        await importer.WriteAsync(row.SourceKey, ct);
        await importer.WriteAsync(row.LanguageCode, ct);
        await importer.WriteAsync(row.LanguageNameEn, ct);
        await importer.WriteAsync(row.LanguageNameAr, ct);
        await importer.WriteAsync(row.NativeName, NpgsqlDbType.Text, ct);
        await importer.WriteAsync(row.Direction, ct);
        await importer.WriteAsync(row.TranslationType, ct);
        await importer.WriteAsync(row.DisplayNameEn, ct);
        await importer.WriteAsync(row.DisplayNameAr, ct);
        await importer.WriteAsync(row.TranslatorKey, NpgsqlDbType.Text, ct);
        await importer.WriteAsync(row.TranslatorNameEn, NpgsqlDbType.Text, ct);
        await importer.WriteAsync(row.TranslatorNameAr, NpgsqlDbType.Text, ct);
        await importer.WriteAsync(row.ContainsInlineFootnotes, ct);
        await importer.WriteAsync(row.ContainsHtmlMarkup, ct);
        await importer.WriteAsync((short)row.ContentCoverageCount, ct);
        await importer.CompleteAsync(ct);
    }

    private static async Task CopyAyahEntryRowsAsync(
        NpgsqlConnection connection,
        int sourceId,
        IReadOnlyList<TranslationAyahEntryDto> ayahEntries,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_translation_ayah_entries (
                source_id,
                ayah_id,
                verse_key,
                text)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var row in ayahEntries)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(sourceId, ct);
            await importer.WriteAsync(row.AyahId, ct);
            await importer.WriteAsync(row.VerseKey, ct);
            await importer.WriteAsync(row.Text, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task<int> ReadSourceIdBySourceKeyAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sourceKey,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(TranslationSql.ReadSourceIdBySourceKey, connection, transaction);
        command.Parameters.AddWithValue("sourceKey", sourceKey);
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }
}
