using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

/// <summary>
/// EF Core implementation of the Unique Words read boundary (Feature 014).
/// Reads are no-tracking and read-only; ayah markers are excluded from
/// occurrence/highlight data. List read (US2); drill-down reads (US3); summary read (US4).
/// </summary>
public sealed class EfUniqueWordsReader(QuranDashboardDbContext db) : IUniqueWordsReader
{
    /// <summary>Total number of surahs in the Quran; used to derive missing-surah counts.</summary>
    private const int TotalSurahs = 114;

    // Arabic symmetric fold. Both the stored column and the user query are
    // folded through the same map so that, e.g., a stored madda alef `آمنوا`
    // matches a plain-alef query `امنوا`, and tashkeel/hamza/ya/alef-maqsura
    // variants fold together. Applied server-side via PostgreSQL translate()
    // on the column and in C# via NormalizeArabicQuery on the query.
    //
    // Only the query side strips tashkeel/tatweel; the column side does not.
    // This is correct because search runs against the imlaei-simple columns
    // (text_imlaei_simple / word_key_imlaei_simple), which are diacritic-free
    // by construction. Searching a column that carried tashkeel would break
    // diacritic-insensitive matching.
    private const string FoldFrom = "أإآؤئةىي";
    private const string FoldTo = "اااواهيي";

    private readonly QuranDashboardDbContext _db = db;

    /// <inheritdoc />
    public async Task<PagedResult<UniqueWordListItemDto>> GetUniqueWordsPageAsync(
        UniqueWordKind kind,
        string? search,
        UniqueWordSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = NormalizeArabicQuery(search);
        var kindKey = kind == UniqueWordKind.Tashkeel
            ? UniqueWordKindKeys.Tashkeel
            : UniqueWordKindKeys.Simple;

        // Build a keyless SqlQueryRaw source, then compose CountAsync /
        // OrderBy / Skip / Take in LINQ so EF/Npgsql wraps it into one
        // parameterized statement with server-side paging. The SQL is fixed
        // (column aliases come from nameof, never user input); only the folded
        // search pattern and the fold map are bound as NpgsqlParameters.
        IQueryable<UniqueWordListRow> rows = kind == UniqueWordKind.Tashkeel
            ? BuildTashkeelQuery(normalizedSearch)
            : BuildSimpleQuery(normalizedSearch);

        rows = ApplySort(rows, sort);

        var totalCount = await rows.CountAsync(cancellationToken);

        var pageRows = await rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = pageRows
            .Select(r => new UniqueWordListItemDto(
                r.Id,
                kindKey,
                r.DisplayTextUthmani,
                r.OccurrencesCount,
                r.AyahsCount,
                r.SurahsCount,
                TotalSurahs - r.SurahsCount,
                $"{r.FirstSurahNumber}:{r.FirstAyahNumber}",
                r.FirstLocation))
            .ToList();

        return new PagedResult<UniqueWordListItemDto>(page, pageSize, totalCount, items);
    }

    /// <inheritdoc />
    public async Task<UniqueWordSummaryDto?> GetUniqueWordSummaryAsync(
        UniqueWordKind kind, int id, CancellationToken cancellationToken)
    {
        // Same projection as the list read, but for a single row by stable ID.
        // The unique-word tables already carry precomputed counts and first
        // occurrence metadata, so the summary needs no per-ayah joins.
        var row = kind == UniqueWordKind.Tashkeel
            ? await _db.QuranWordsUniqueTashkeel
                .AsNoTracking()
                .Where(w => w.Id == id)
                .Select(w => new UniqueWordSummaryRow(
                    w.TextUthmani,
                    UniqueWordKindKeys.Tashkeel,
                    w.OccurrencesCount,
                    w.AyahsCount,
                    w.SurahsCount,
                    w.FirstSurahNumber,
                    w.FirstAyahNumber,
                    w.FirstLocation))
                .FirstOrDefaultAsync(cancellationToken)
            : await _db.QuranWordsUniqueSimple
                .AsNoTracking()
                .Where(w => w.Id == id)
                .Select(w => new UniqueWordSummaryRow(
                    w.TextUthmani,
                    UniqueWordKindKeys.Simple,
                    w.OccurrencesCount,
                    w.AyahsCount,
                    w.SurahsCount,
                    w.FirstSurahNumber,
                    w.FirstAyahNumber,
                    w.FirstLocation))
                .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new UniqueWordSummaryDto(
            id,
            row.KindKey,
            row.DisplayTextUthmani,
            row.OccurrencesCount,
            row.AyahsCount,
            row.SurahsCount,
            TotalSurahs - row.SurahsCount,
            $"{row.FirstSurahNumber}:{row.FirstAyahNumber}",
            row.FirstLocation);
    }

    /// <inheritdoc />
    public async Task<UniqueWordSurahsResponse?> GetMentionedSurahsAsync(
        UniqueWordKind kind, int id, CancellationToken cancellationToken)
    {
        var header = await LoadUniqueWordHeaderAsync(kind, id, cancellationToken);
        if (header is null)
        {
            return null;
        }

        var surahGroups = await ReadableMatchesQuery(kind, id)
            .GroupBy(w => w.SurahNumber)
            .Select(g => new SurahOccurrenceRow(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        surahGroups = surahGroups.OrderBy(r => r.SurahNumber).ToList();

        if (surahGroups.Count == 0)
        {
            return new UniqueWordSurahsResponse(
                id,
                header.KindKey,
                header.DisplayTextUthmani,
                0,
                []);
        }

        var surahNumbers = surahGroups.Select(r => r.SurahNumber).ToList();
        var surahNames = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => surahNumbers.Contains(s.SurahNumber))
            .ToDictionaryAsync(s => s.SurahNumber, s => s.NameArabic, cancellationToken);

        var surahs = surahGroups
            .Select(r => new UniqueWordSurahItemDto(
                r.SurahNumber,
                surahNames[r.SurahNumber],
                r.OccurrencesInSurah))
            .ToList();

        return new UniqueWordSurahsResponse(
            id,
            header.KindKey,
            header.DisplayTextUthmani,
            surahs.Count,
            surahs);
    }

    /// <inheritdoc />
    public async Task<UniqueWordMissingSurahsResponse?> GetMissingSurahsAsync(
        UniqueWordKind kind, int id, CancellationToken cancellationToken)
    {
        var header = await LoadUniqueWordHeaderAsync(kind, id, cancellationToken);
        if (header is null)
        {
            return null;
        }

        var mentionedSurahNumbers = await ReadableMatchesQuery(kind, id)
            .Select(w => w.SurahNumber)
            .Distinct()
            .ToListAsync(cancellationToken);

        var missingSurahs = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => !mentionedSurahNumbers.Contains(s.SurahNumber))
            .OrderBy(s => s.SurahNumber)
            .Select(s => new MissingSurahItemDto(s.SurahNumber, s.NameArabic))
            .ToListAsync(cancellationToken);

        return new UniqueWordMissingSurahsResponse(
            id,
            header.KindKey,
            header.DisplayTextUthmani,
            missingSurahs.Count,
            missingSurahs);
    }

    /// <inheritdoc />
    public async Task<PagedResult<UniqueWordAyahMatchDto>?> GetAyahMatchesAsync(
        UniqueWordKind kind, int id, int page, int pageSize, CancellationToken cancellationToken)
    {
        var header = await LoadUniqueWordHeaderAsync(kind, id, cancellationToken);
        if (header is null)
        {
            return null;
        }

        var matchedAyahIds = ReadableMatchesQuery(kind, id).Select(w => w.AyahId).Distinct();

        var totalCount = await matchedAyahIds.CountAsync(cancellationToken);

        var pageAyahs = await _db.QuranAyahs
            .AsNoTracking()
            .Where(a => matchedAyahIds.Contains(a.Id))
            .OrderBy(a => a.SurahNumber)
            .ThenBy(a => a.AyahNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new DistinctAyahRow(a.Id, a.SurahNumber, a.AyahNumber))
            .ToListAsync(cancellationToken);

        if (pageAyahs.Count == 0)
        {
            return new PagedResult<UniqueWordAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var ayahIds = pageAyahs.Select(a => a.AyahId).ToList();

        var matchedRows = await ReadableMatchesQuery(kind, id)
            .Where(w => ayahIds.Contains(w.AyahId))
            .Select(w => new { w.AyahId, w.Id })
            .ToListAsync(cancellationToken);

        var matchedIdsByAyah = matchedRows
            .GroupBy(r => r.AyahId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<int>)g.Select(x => x.Id).ToList());

        var ayahMeta = await (
            from ayah in _db.QuranAyahs.AsNoTracking()
            join surah in _db.QuranSurahs.AsNoTracking()
                on ayah.SurahNumber equals surah.SurahNumber
            where ayahIds.Contains(ayah.Id)
            select new AyahMetaRow(
                ayah.Id,
                ayah.VerseKey,
                ayah.SurahNumber,
                ayah.AyahNumber,
                surah.NameArabic))
            .ToDictionaryAsync(a => a.AyahId, cancellationToken);

        var wordsByAyah = await _db.QuranWords
            .AsNoTracking()
            .Where(w => ayahIds.Contains(w.AyahId))
            .OrderBy(w => w.SurahNumber)
            .ThenBy(w => w.AyahNumber)
            .ThenBy(w => w.WordNumber)
            .Select(w => new AyahWordRow(
                w.AyahId,
                w.Id,
                w.WordNumber,
                w.TextUthmani,
                w.IsAyahMarker))
            .ToListAsync(cancellationToken);

        var wordsGrouped = wordsByAyah
            .GroupBy(w => w.AyahId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = pageAyahs
            .Select(ayah =>
            {
                var meta = ayahMeta[ayah.AyahId];
                var words = wordsGrouped.GetValueOrDefault(ayah.AyahId, []);
                return new UniqueWordAyahMatchDto(
                    meta.AyahId,
                    meta.VerseKey,
                    meta.SurahNumber,
                    meta.SurahNameArabic,
                    meta.AyahNumber,
                    matchedIdsByAyah.GetValueOrDefault(ayah.AyahId, []),
                    words.Select(w => new AyahWordForHighlightDto(
                        w.QuranWordId,
                        w.WordNumber,
                        w.TextUthmani,
                        w.IsAyahMarker)).ToList());
            })
            .ToList();

        return new PagedResult<UniqueWordAyahMatchDto>(page, pageSize, totalCount, items);
    }

    private IQueryable<QuranWord> ReadableMatchesQuery(UniqueWordKind kind, int id) =>
        kind == UniqueWordKind.Tashkeel
            ? _db.QuranWords.AsNoTracking()
                .Where(w => !w.IsAyahMarker && w.UniqueTashkeelWordId == id)
            : _db.QuranWords.AsNoTracking()
                .Where(w => !w.IsAyahMarker && w.UniqueSimpleWordId == id);

    private async Task<UniqueWordHeaderRow?> LoadUniqueWordHeaderAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken)
    {
        if (kind == UniqueWordKind.Tashkeel)
        {
            var row = await _db.QuranWordsUniqueTashkeel
                .AsNoTracking()
                .Where(w => w.Id == id)
                .Select(w => new UniqueWordHeaderRow(
                    w.TextUthmani,
                    UniqueWordKindKeys.Tashkeel))
                .FirstOrDefaultAsync(cancellationToken);

            return row;
        }

        var simpleRow = await _db.QuranWordsUniqueSimple
            .AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new UniqueWordHeaderRow(
                w.TextUthmani,
                UniqueWordKindKeys.Simple))
            .FirstOrDefaultAsync(cancellationToken);

        return simpleRow;
    }

    private IQueryable<UniqueWordListRow> BuildTashkeelQuery(string? normalizedSearch)
    {
        // translate() folds the stored text the same way NormalizeArabicQuery
        // folds the user query, so matching is symmetric. ilike gives
        // case-insensitive contains. The search pattern is parameterized; the
        // fold map is a fixed literal.
        //
        // Column aliases are double-quoted so PostgreSQL preserves the exact
        // PascalCase casing that EF Core maps against the UniqueWordListRow
        // properties. Unquoted aliases would be lowercased and fail to bind.
        var sql = $"""
            SELECT
                id AS "{nameof(UniqueWordListRow.Id)}",
                text_uthmani AS "{nameof(UniqueWordListRow.DisplayTextUthmani)}",
                occurrences_count AS "{nameof(UniqueWordListRow.OccurrencesCount)}",
                ayahs_count AS "{nameof(UniqueWordListRow.AyahsCount)}",
                surahs_count AS "{nameof(UniqueWordListRow.SurahsCount)}",
                first_surah_number AS "{nameof(UniqueWordListRow.FirstSurahNumber)}",
                first_ayah_number AS "{nameof(UniqueWordListRow.FirstAyahNumber)}",
                first_location AS "{nameof(UniqueWordListRow.FirstLocation)}",
                first_word_order_in_mushaf AS "{nameof(UniqueWordListRow.FirstWordOrderInMushaf)}",
                text_imlaei_simple AS "{nameof(UniqueWordListRow.SearchText)}"
            FROM quran_words_unique_tashkeel
            """;

        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            var pattern = $"%{EscapeLikePattern(normalizedSearch)}%";
            sql += " WHERE translate(lower(text_imlaei_simple), @foldFrom, @foldTo) ILIKE @pattern";
            return _db.Database.SqlQueryRaw<UniqueWordListRow>(
                sql,
                new NpgsqlParameter("foldFrom", FoldFrom),
                new NpgsqlParameter("foldTo", FoldTo),
                new NpgsqlParameter("pattern", pattern));
        }

        return _db.Database.SqlQueryRaw<UniqueWordListRow>(sql);
    }

    private IQueryable<UniqueWordListRow> BuildSimpleQuery(string? normalizedSearch)
    {
        var sql = $"""
            SELECT
                id AS "{nameof(UniqueWordListRow.Id)}",
                text_uthmani AS "{nameof(UniqueWordListRow.DisplayTextUthmani)}",
                occurrences_count AS "{nameof(UniqueWordListRow.OccurrencesCount)}",
                ayahs_count AS "{nameof(UniqueWordListRow.AyahsCount)}",
                surahs_count AS "{nameof(UniqueWordListRow.SurahsCount)}",
                first_surah_number AS "{nameof(UniqueWordListRow.FirstSurahNumber)}",
                first_ayah_number AS "{nameof(UniqueWordListRow.FirstAyahNumber)}",
                first_location AS "{nameof(UniqueWordListRow.FirstLocation)}",
                first_word_order_in_mushaf AS "{nameof(UniqueWordListRow.FirstWordOrderInMushaf)}",
                word_key_imlaei_simple AS "{nameof(UniqueWordListRow.SearchText)}"
            FROM quran_words_unique_simple
            """;

        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            var pattern = $"%{EscapeLikePattern(normalizedSearch)}%";
            sql += " WHERE translate(lower(word_key_imlaei_simple), @foldFrom, @foldTo) ILIKE @pattern";
            return _db.Database.SqlQueryRaw<UniqueWordListRow>(
                sql,
                new NpgsqlParameter("foldFrom", FoldFrom),
                new NpgsqlParameter("foldTo", FoldTo),
                new NpgsqlParameter("pattern", pattern));
        }

        return _db.Database.SqlQueryRaw<UniqueWordListRow>(sql);
    }

    private static IQueryable<UniqueWordListRow> ApplySort(IQueryable<UniqueWordListRow> rows, UniqueWordSort sort) => sort switch
    {
        UniqueWordSort.Occurrences => rows
            .OrderByDescending(r => r.OccurrencesCount)
            .ThenBy(r => r.FirstWordOrderInMushaf),
        UniqueWordSort.Alpha => rows
            .OrderBy(r => r.SearchText)
            .ThenBy(r => r.FirstWordOrderInMushaf),
        _ => rows.OrderBy(r => r.FirstWordOrderInMushaf),
    };

    /// <summary>
    /// Folds an Arabic search query the same way the SQL <c>translate()</c>
    /// folds the stored column: strip tashkeel/tatweel, then map the common
    /// letter-form variants to their canonical base form. Returns
    /// <see langword="null"/> for blank input (meaning "no search filter").
    /// </summary>
    private static string? NormalizeArabicQuery(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var builder = new StringBuilder(search.Length);
        foreach (var ch in search)
        {
            // Skip Arabic diacritics (tashkeel), the alef madda superscript
            // alef (U+0670), and tatweel (U+0640) so diacritic-insensitive
            // matching matches the unvoweled stored column.
            if (IsSkippable(ch))
            {
                continue;
            }

            var folded = Fold(ch);
            builder.Append(folded);
        }

        var normalized = builder.ToString().ToLowerInvariant();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static bool IsSkippable(char ch) =>
        ch is
            '\u064B' or '\u064C' or '\u064D' or '\u064E' or '\u064F' or
            '\u0650' or '\u0651' or '\u0652' or '\u0670' or '\u0640';

    private static char Fold(char ch)
    {
        var index = FoldFrom.IndexOf(ch);
        return index >= 0 ? FoldTo[index] : ch;
    }

    /// <summary>
    /// Escapes the ILIKE wildcard characters (<c>%</c>, <c>_</c>) and the
    /// default escape character <c>\</c> in the user query so they match
    /// literally. The folded query may legitimately contain <c>_</c> (no
    /// Arabic letter maps to it, but guard regardless).
    /// </summary>
    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    /// <summary>
    /// Projected row carrying only the columns the list DTO consumes, plus a
    /// <see cref="SearchText"/> column used for alpha sort. Avoids materializing
    /// the full entity graph.
    /// </summary>
    private sealed record UniqueWordListRow(
        int Id,
        string DisplayTextUthmani,
        int OccurrencesCount,
        short AyahsCount,
        short SurahsCount,
        short FirstSurahNumber,
        short FirstAyahNumber,
        string FirstLocation,
        int FirstWordOrderInMushaf,
        string SearchText);

    private sealed record UniqueWordHeaderRow(string DisplayTextUthmani, string KindKey);

    /// <summary>
    /// Single-row projection for the summary read; carries only the columns the
    /// summary DTO consumes. Shares shape with the list row but is read by ID.
    /// </summary>
    private sealed record UniqueWordSummaryRow(
        string DisplayTextUthmani,
        string KindKey,
        int OccurrencesCount,
        short AyahsCount,
        short SurahsCount,
        short FirstSurahNumber,
        short FirstAyahNumber,
        string FirstLocation);

    private sealed record SurahOccurrenceRow(short SurahNumber, int OccurrencesInSurah);

    private sealed record DistinctAyahRow(int AyahId, short SurahNumber, short AyahNumber);

    private sealed record AyahMetaRow(
        int AyahId,
        string VerseKey,
        short SurahNumber,
        short AyahNumber,
        string SurahNameArabic);

    private sealed record AyahWordRow(
        int AyahId,
        int QuranWordId,
        short WordNumber,
        string TextUthmani,
        bool IsAyahMarker);
}
