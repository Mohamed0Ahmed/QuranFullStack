using System.Linq.Expressions;
using QuranDashboard.Application.Abstractions.Common.Filtering;
using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

// List-query building for the Unique Words list read: the raw SELECTs per kind, the composed
// WHERE clause (search + count ranges + association winner predicates), and the list sort.
// Split out of EfUniqueWordsReader.cs by size (same partial convention as EfStemsReader).
public sealed partial class EfUniqueWordsReader
{
    // Allowlisted foreign-key columns on quran_words that link a readable word to its unique identity;
    // chosen only by the two Build* call sites below, never from user input.
    private const string TashkeelIdColumn = "unique_tashkeel_word_id";
    private const string SimpleIdColumn = "unique_simple_word_id";

    private IQueryable<UniqueWordListRow> BuildTashkeelQuery(string? normalizedSearch, UniqueWordsCountFilter filter, UniqueWordsAssociationFilter association)
    {
        var sql = $"""
            SELECT
                id AS "{nameof(UniqueWordListRow.Id)}",
                text_uthmani AS "{nameof(UniqueWordListRow.DisplayText)}",
                occurrences_count AS "{nameof(UniqueWordListRow.OccurrencesCount)}",
                ayahs_count AS "{nameof(UniqueWordListRow.AyahsCount)}",
                surahs_count AS "{nameof(UniqueWordListRow.SurahsCount)}",
                first_word_order_in_mushaf AS "{nameof(UniqueWordListRow.FirstWordOrderInMushaf)}",
                text_imlaei_simple AS "{nameof(UniqueWordListRow.SearchText)}"
            FROM quran_words_unique_tashkeel
            """;

        return BuildListQuery(sql, TashkeelIdColumn, normalizedSearch, filter, association);
    }

    private IQueryable<UniqueWordListRow> BuildSimpleQuery(string? normalizedSearch, UniqueWordsCountFilter filter, UniqueWordsAssociationFilter association)
    {
        var sql = $"""
            SELECT
                id AS "{nameof(UniqueWordListRow.Id)}",
                text_uthmani_simple AS "{nameof(UniqueWordListRow.DisplayText)}",
                occurrences_count AS "{nameof(UniqueWordListRow.OccurrencesCount)}",
                ayahs_count AS "{nameof(UniqueWordListRow.AyahsCount)}",
                surahs_count AS "{nameof(UniqueWordListRow.SurahsCount)}",
                first_word_order_in_mushaf AS "{nameof(UniqueWordListRow.FirstWordOrderInMushaf)}",
                word_key_imlaei_simple AS "{nameof(UniqueWordListRow.SearchText)}"
            FROM quran_words_unique_simple
            """;

        return BuildListQuery(sql, SimpleIdColumn, normalizedSearch, filter, association);
    }

    // Composes the optional search predicate, the count-range predicates (Feature 026, US5) and the
    // association predicates (Feature 026, US7) into a single WHERE clause. Column identifiers are
    // hardcoded/allowlisted; the search fragment, every range bound, and the association values reach
    // SQL only as parameter values. Both unique tables expose the same
    // occurrences_count/ayahs_count/surahs_count columns and the search_text_normalized identity column.
    private IQueryable<UniqueWordListRow> BuildListQuery(
        string sql,
        string uniqueIdColumn,
        string? normalizedSearch,
        UniqueWordsCountFilter filter,
        UniqueWordsAssociationFilter association)
    {
        var conditions = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            var pattern = $"%{ArabicSearchQueryNormalizer.EscapeLikePattern(normalizedSearch)}%";
            conditions.Add("search_text_normalized ILIKE @pattern");
            parameters.Add(new NpgsqlParameter("pattern", pattern));
        }

        AppendRange(conditions, parameters, "occurrences_count", "occ", filter.Occurrences);
        AppendRange(conditions, parameters, "ayahs_count", "ayahs", filter.Ayahs);
        AppendRange(conditions, parameters, "surahs_count", "surahs", filter.Surahs);

        if (association.NormalizedPrimaryType is { } primaryType)
        {
            conditions.Add(PrimaryWordTypeWinnerPredicate(uniqueIdColumn));
            parameters.Add(new NpgsqlParameter("primaryType", primaryType));
        }

        if (association.RootId is int rootId)
        {
            conditions.Add(PrimaryRootWinnerPredicate(uniqueIdColumn));
            parameters.Add(new NpgsqlParameter("assocRootId", rootId));
        }

        if (conditions.Count > 0)
        {
            sql += " WHERE " + string.Join(" AND ", conditions);
        }

        return parameters.Count > 0
            ? _db.Database.SqlQueryRaw<UniqueWordListRow>(sql, [.. parameters])
            : _db.Database.SqlQueryRaw<UniqueWordListRow>(sql);
    }

    // Keeps only unique words whose PRIMARY word type equals @primaryType. The winner is selected by the
    // SAME rule LoadPrimaryWordTypesAsync (EfUniqueWordsReader.cs) uses for the displayed chip — group
    // the word's occurrences by POS code, then order by occurrence count DESC, earliest quran_word id
    // ASC, POS code (ordinal) — so the displayed primary type and the filter can never disagree (the
    // chip⇔filter invariant, pinned by UniqueWordsAssociationFilterTests). LOCKSTEP: any change to the
    // enrichment's selection rule MUST be mirrored here (and vice versa). uniqueIdColumn is an
    // allowlisted constant, never user input.
    private static string PrimaryWordTypeWinnerPredicate(string uniqueIdColumn) => $"""
        id IN (
            SELECT winner.unique_id
            FROM (
                SELECT DISTINCT ON (grp.unique_id) grp.unique_id, grp.code
                FROM (
                    SELECT qw.{uniqueIdColumn} AS unique_id, pt.code AS code,
                           COUNT(*) AS occ, MIN(qw.id) AS earliest
                    FROM quran_words qw
                    JOIN quran_word_morphology m ON m.quran_word_id = qw.id
                    JOIN quran_pos_tags pt ON pt.code = m.head_pos
                    WHERE qw.{uniqueIdColumn} IS NOT NULL
                    GROUP BY qw.{uniqueIdColumn}, pt.code
                ) grp
                ORDER BY grp.unique_id, grp.occ DESC, grp.earliest, grp.code COLLATE "C"
            ) winner
            WHERE winner.code = @primaryType
        )
        """;

    // Keeps only unique words whose PRIMARY root equals @assocRootId, selected by the SAME rule
    // LoadPrimaryRootsAsync (EfUniqueWordsReader.cs) uses for the displayed chip (occurrence count DESC,
    // earliest quran_word id ASC, root id ASC). LOCKSTEP: any change to the enrichment's selection rule
    // MUST be mirrored here (and vice versa). uniqueIdColumn is an allowlisted constant, never user
    // input.
    private static string PrimaryRootWinnerPredicate(string uniqueIdColumn) => $"""
        id IN (
            SELECT winner.unique_id
            FROM (
                SELECT DISTINCT ON (grp.unique_id) grp.unique_id, grp.root_id
                FROM (
                    SELECT qw.{uniqueIdColumn} AS unique_id, root.id AS root_id,
                           COUNT(*) AS occ, MIN(qw.id) AS earliest
                    FROM quran_words qw
                    JOIN quran_word_morphology m ON m.quran_word_id = qw.id
                    JOIN quran_roots root ON root.id = m.root_id
                    WHERE qw.{uniqueIdColumn} IS NOT NULL
                    GROUP BY qw.{uniqueIdColumn}, root.id
                ) grp
                ORDER BY grp.unique_id, grp.occ DESC, grp.earliest, grp.root_id
            ) winner
            WHERE winner.root_id = @assocRootId
        )
        """;

    private static void AppendRange(
        List<string> conditions,
        List<NpgsqlParameter> parameters,
        string column,
        string parameterPrefix,
        CountRange range)
    {
        if (range.Min is int min)
        {
            conditions.Add($"{column} >= @{parameterPrefix}Min");
            parameters.Add(new NpgsqlParameter($"{parameterPrefix}Min", min));
        }

        if (range.Max is int max)
        {
            conditions.Add($"{column} <= @{parameterPrefix}Max");
            parameters.Add(new NpgsqlParameter($"{parameterPrefix}Max", max));
        }
    }

    // Ordering is part of the read contract (see the reads README) and runs BEFORE Count/Skip/Take, so it
    // decides paging. Every branch now ends on Id: FirstWordOrderInMushaf is already effectively unique,
    // so this changes no existing result — it closes a DB-side non-determinism gap (unlike the in-memory
    // explorers, a LINQ-to-SQL ORDER BY has no stable-sort net). Each tie-break chain is identical in
    // BOTH directions, so reversing a column never reshuffles its ties.
    private static IQueryable<UniqueWordListRow> ApplySort(IQueryable<UniqueWordListRow> rows, UniqueWordSortSpec sort) => sort.Column switch
    {
        UniqueWordSortColumn.Alpha => Ordered(rows, r => r.SearchText, sort.Direction),
        UniqueWordSortColumn.Occurrences => Ordered(rows, r => r.OccurrencesCount, sort.Direction),
        UniqueWordSortColumn.Ayahs => Ordered(rows, r => r.AyahsCount, sort.Direction),
        UniqueWordSortColumn.Surahs => Ordered(rows, r => r.SurahsCount, sort.Direction),
        UniqueWordSortColumn.MushafOrder => rows
            .OrderBy(r => r.FirstWordOrderInMushaf)
            .ThenBy(r => r.Id),
        // Explicit, so a column added without an arm here fails loudly instead of silently
        // serving Mushaf order (mirrors the word-types SQL switches).
        _ => throw new InvalidOperationException($"Unhandled {nameof(UniqueWordSortColumn)} value: {sort.Column}."),
    };

    private static IQueryable<UniqueWordListRow> Ordered<TKey>(
        IQueryable<UniqueWordListRow> rows,
        Expression<Func<UniqueWordListRow, TKey>> key,
        WordSortDirection direction) =>
        (direction == WordSortDirection.Ascending ? rows.OrderBy(key) : rows.OrderByDescending(key))
            .ThenBy(r => r.FirstWordOrderInMushaf)
            .ThenBy(r => r.Id);

    private sealed record UniqueWordListRow(
        int Id,
        string DisplayText,
        int OccurrencesCount,
        short AyahsCount,
        short SurahsCount,
        int FirstWordOrderInMushaf,
        string SearchText);
}
