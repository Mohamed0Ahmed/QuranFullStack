using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

/// <summary>
/// EF Core implementation of the Unique Words read boundary (Feature 014).
/// Reads are no-tracking and read-only; ayah markers are excluded from
/// occurrence/highlight data. List read (US2) is implemented in T038; the four
/// drill-down/summary methods remain compile-safe stubs until US3/US4.
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
    public Task<UniqueWordSummaryDto?> GetUniqueWordSummaryAsync(
        UniqueWordKind kind, int id, CancellationToken cancellationToken)
        => throw new NotImplementedException("Implemented in T080.");

    /// <inheritdoc />
    public Task<UniqueWordSurahsResponse?> GetMentionedSurahsAsync(
        UniqueWordKind kind, int id, CancellationToken cancellationToken)
        => throw new NotImplementedException("Implemented in T063.");

    /// <inheritdoc />
    public Task<UniqueWordMissingSurahsResponse?> GetMissingSurahsAsync(
        UniqueWordKind kind, int id, CancellationToken cancellationToken)
        => throw new NotImplementedException("Implemented in T063.");

    /// <inheritdoc />
    public Task<PagedResult<UniqueWordAyahMatchDto>?> GetAyahMatchesAsync(
        UniqueWordKind kind, int id, int page, int pageSize, CancellationToken cancellationToken)
        => throw new NotImplementedException("Implemented in T063.");

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
}
