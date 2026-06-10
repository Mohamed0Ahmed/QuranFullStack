using System.Data;
using Npgsql;
using QuranDashboard.Application.Abstractions.Quran.Import;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Import;

public sealed class EfBulkQuranImportWriter : IQuranImportWriter
{
    private readonly QuranDashboardDbContext dbContext;

    public EfBulkQuranImportWriter(QuranDashboardDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct)
    {
        return await dbContext.QuranSurahs.AnyAsync(ct)
            || await dbContext.QuranAyahs.AnyAsync(ct)
            || await dbContext.QuranMushafPages.AnyAsync(ct)
            || await dbContext.QuranMushafLines.AnyAsync(ct)
            || await dbContext.QuranWords.AnyAsync(ct);
    }

    public async Task WriteAsync(AssembledQuranData data, bool force, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(data);
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for bulk import.");
        }

        if (!force && await AnyTargetTableHasDataAsync(ct))
        {
            throw new InvalidOperationException(ImportRefusalMessages.TablesNotEmpty);
        }

        await using var transaction = await npgsqlConnection.BeginTransactionAsync(ct);

        try
        {
            if (force)
            {
                await TruncateTargetTablesAsync(npgsqlConnection, ct);
            }

            await CopySurahsAsync(npgsqlConnection, data.Surahs, ct);
            await CopyAyahsAsync(npgsqlConnection, data.Ayahs, ct);
            await CopyPagesAsync(npgsqlConnection, data.Pages, ct);
            await CopyWordsAsync(npgsqlConnection, data.Words, ct);
            await CopyLinesAsync(npgsqlConnection, data.Lines, ct);

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task CopySurahsAsync(
        NpgsqlConnection connection,
        IReadOnlyList<Surah> surahs,
        CancellationToken ct)
    {
        const string copyCommand =
            "COPY quran_surahs (surah_number, name_arabic, name_simple, name_transliteration, revelation_place, revelation_order, verses_count, bismillah_pre) FROM STDIN (FORMAT BINARY)";

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var surah in surahs)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(surah.SurahNumber, ct);
            await importer.WriteAsync(surah.NameArabic, ct);
            await importer.WriteAsync(surah.NameSimple, ct);
            await importer.WriteAsync(surah.NameTransliteration, ct);
            await importer.WriteAsync(ToRevelationPlaceValue(surah.RevelationPlace), ct);
            await importer.WriteAsync(surah.RevelationOrder, ct);
            await importer.WriteAsync(surah.VersesCount, ct);
            await importer.WriteAsync(surah.BismillahPre, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopyAyahsAsync(
        NpgsqlConnection connection,
        IReadOnlyList<Ayah> ayahs,
        CancellationToken ct)
    {
        const string copyCommand =
            "COPY quran_ayahs (id, surah_number, ayah_number, verse_key, text_uthmani, words_count_source, words_count_real, page_from, page_to) FROM STDIN (FORMAT BINARY)";

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var ayah in ayahs)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(ayah.Id, ct);
            await importer.WriteAsync(ayah.SurahNumber, ct);
            await importer.WriteAsync(ayah.AyahNumber, ct);
            await importer.WriteAsync(ayah.VerseKey, ct);
            await importer.WriteAsync(ayah.TextUthmani, ct);
            await importer.WriteAsync(ayah.WordsCountSource, ct);
            await importer.WriteAsync(ayah.WordsCountReal, ct);
            await importer.WriteAsync(ayah.PageFrom, ct);
            await importer.WriteAsync(ayah.PageTo, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopyPagesAsync(
        NpgsqlConnection connection,
        IReadOnlyList<MushafPage> pages,
        CancellationToken ct)
    {
        const string copyCommand =
            "COPY quran_mushaf_pages (page_number, first_surah_number, first_ayah_number, last_surah_number, last_ayah_number, lines_count) FROM STDIN (FORMAT BINARY)";

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var page in pages)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(page.PageNumber, ct);
            await importer.WriteAsync(page.FirstSurahNumber, ct);
            await importer.WriteAsync(page.FirstAyahNumber, ct);
            await importer.WriteAsync(page.LastSurahNumber, ct);
            await importer.WriteAsync(page.LastAyahNumber, ct);
            await importer.WriteAsync(page.LinesCount, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopyWordsAsync(
        NpgsqlConnection connection,
        IReadOnlyList<QuranWord> words,
        CancellationToken ct)
    {
        const string copyCommand =
            "COPY quran_words (id, location, ayah_id, surah_number, ayah_number, word_number, page_number, line_number, line_word_order, qpc_glyph, text_uthmani, text_uthmani_simple, text_imlaei_simple, word_key_imlaei_simple, is_ayah_marker) FROM STDIN (FORMAT BINARY)";

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var word in words)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(word.Id, ct);
            await importer.WriteAsync(word.Location, ct);
            await importer.WriteAsync(word.AyahId, ct);
            await importer.WriteAsync(word.SurahNumber, ct);
            await importer.WriteAsync(word.AyahNumber, ct);
            await importer.WriteAsync(word.WordNumber, ct);
            await importer.WriteAsync(word.PageNumber, ct);
            await importer.WriteAsync(word.LineNumber, ct);
            await importer.WriteAsync(word.LineWordOrder, ct);
            await importer.WriteAsync(word.QpcGlyph, ct);
            await importer.WriteAsync(word.TextUthmani, ct);
            await importer.WriteAsync(word.TextUthmaniSimple, ct);
            await importer.WriteAsync(word.TextImlaeiSimple, ct);
            await importer.WriteAsync(word.WordKeyImlaeiSimple, ct);
            await importer.WriteAsync(word.IsAyahMarker, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopyLinesAsync(
        NpgsqlConnection connection,
        IReadOnlyList<MushafLine> lines,
        CancellationToken ct)
    {
        const string copyCommand =
            "COPY quran_mushaf_lines (page_number, line_number, line_type, is_centered, surah_number, first_word_id, last_word_id, words_count) FROM STDIN (FORMAT BINARY)";

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var line in lines)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(line.PageNumber, ct);
            await importer.WriteAsync(line.LineNumber, ct);
            await importer.WriteAsync(ToLineTypeValue(line.LineType), ct);
            await importer.WriteAsync(line.IsCentered, ct);
            await importer.WriteAsync(line.SurahNumber, ct);
            await importer.WriteAsync(line.FirstWordId, ct);
            await importer.WriteAsync(line.LastWordId, ct);
            await importer.WriteAsync(line.WordsCount, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task TruncateTargetTablesAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "TRUNCATE quran_words, quran_mushaf_lines, quran_mushaf_pages, quran_ayahs, quran_surahs RESTART IDENTITY CASCADE";
        await command.ExecuteNonQueryAsync(ct);
    }

    private static string ToRevelationPlaceValue(RevelationPlace value) => value switch
    {
        RevelationPlace.Makkah => "makkah",
        RevelationPlace.Madinah => "madinah",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown revelation place.")
    };

    private static string ToLineTypeValue(MushafLineType value) => value switch
    {
        MushafLineType.Ayah => "ayah",
        MushafLineType.SurahName => "surah_name",
        MushafLineType.Basmallah => "basmallah",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown mushaf line type.")
    };
}
