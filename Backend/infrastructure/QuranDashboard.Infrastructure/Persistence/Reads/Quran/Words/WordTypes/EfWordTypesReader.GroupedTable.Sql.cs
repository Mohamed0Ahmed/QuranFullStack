using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

// Grouped table-view (roots/stems/lemmas) SQL: the query/count shapes, the alpha fold gate, the
// ORDER BY, the dimension column map, the parameter builder, and the row record. Split out of
// EfWordTypesReader.Sql.cs, which keeps the shared base and the Words-view reads. Behaviour is
// unchanged — these members were moved verbatim.
public sealed partial class EfWordTypesReader
{
    // Grouped table views (roots/stems/lemmas) reuse BaseRowsSql verbatim and group by the numeric
    // dimension ID, excluding nulls. Grouping and total counting happen before pagination.
    private static string GroupedRowsSql(WordTypeReadContext context, WordTypeTableView view, WordTypeSortSpec sort)
    {
        var (idColumn, textColumn) = DimensionColumns(view);
        var normTextColumn = NeedsFold(sort)
            ? $", replace(translate(lower(MIN({textColumn})), @foldFrom, @foldTo), ' ', '') AS norm_text"
            : string.Empty;

        return $"""
        WITH base AS (
            {BaseRowsSql(context)}
        ), grouped AS (
            SELECT
                {idColumn} AS dimension_id,
                MIN({textColumn}) AS display_text,
                MIN(quran_word_id) AS first_word_order_in_mushaf,
                COUNT(*)::int AS occurrences_count,
                COUNT(DISTINCT ayah_id)::int AS ayahs_count,
                COUNT(DISTINCT surah_number)::int AS surahs_count
                {normTextColumn}
            FROM base
            WHERE {idColumn} IS NOT NULL
            GROUP BY {idColumn}
        )
        SELECT
            dimension_id AS "{nameof(GroupedRowSqlResult.DimensionId)}",
            display_text AS "{nameof(GroupedRowSqlResult.DisplayText)}",
            occurrences_count AS "{nameof(GroupedRowSqlResult.OccurrencesCount)}",
            ayahs_count AS "{nameof(GroupedRowSqlResult.AyahsCount)}",
            surahs_count AS "{nameof(GroupedRowSqlResult.SurahsCount)}",
            first_word_order_in_mushaf AS "{nameof(GroupedRowSqlResult.FirstWordOrderInMushaf)}",
            -- Window count over the distinct non-null dimension rows, so page + total come from ONE
            -- command; equals GroupedRowsCountSql's COUNT(DISTINCT {idColumn}) for the identical scope.
            COUNT(*) OVER()::int AS "{nameof(GroupedRowSqlResult.TotalCount)}"
        FROM grouped
        ORDER BY {GroupedOrderBy(sort)}
        OFFSET @skip LIMIT @take
        """;
    }

    // THE single fold predicate. The grouped alpha ORDER BY reads the folded `norm_text` column, which
    // only exists when GroupedRowsSql projects it AND @foldFrom/@foldTo are bound. Both sites gate on
    // this one method: if they ever disagree, the query either sorts on a missing column or Npgsql
    // rejects an unbound parameter — at RUNTIME. Alpha in EITHER direction folds.
    private static bool NeedsFold(WordTypeSortSpec sort) => sort.Column == WordTypeSortColumn.Alpha;

    // Grouped totalCount = distinct non-null dimension IDs over the scoped base, measured before paging.
    private static string GroupedRowsCountSql(WordTypeReadContext context, WordTypeTableView view)
    {
        var (idColumn, _) = DimensionColumns(view);
        return $"""
        WITH base AS (
            {BaseRowsSql(context)}
        )
        SELECT COUNT(DISTINCT {idColumn})::int AS "{nameof(CountRow.Count)}"
        FROM base
        WHERE {idColumn} IS NOT NULL
        """;
    }

    // Grouped-view ORDER BY. Same constant-only rule as OrderBy in EfWordTypesReader.Sql.cs. The alpha
    // arms read the folded norm_text column that NeedsFold gates into the CTE.
    private static string GroupedOrderBy(WordTypeSortSpec sort) => (sort.Column, sort.Direction) switch
    {
        (WordTypeSortColumn.Occurrences, WordSortDirection.Descending) => "occurrences_count DESC, first_word_order_in_mushaf, dimension_id",
        (WordTypeSortColumn.Occurrences, WordSortDirection.Ascending) => "occurrences_count, first_word_order_in_mushaf, dimension_id",
        (WordTypeSortColumn.Ayahs, WordSortDirection.Descending) => "ayahs_count DESC, first_word_order_in_mushaf, dimension_id",
        (WordTypeSortColumn.Ayahs, WordSortDirection.Ascending) => "ayahs_count, first_word_order_in_mushaf, dimension_id",
        (WordTypeSortColumn.Surahs, WordSortDirection.Descending) => "surahs_count DESC, first_word_order_in_mushaf, dimension_id",
        (WordTypeSortColumn.Surahs, WordSortDirection.Ascending) => "surahs_count, first_word_order_in_mushaf, dimension_id",
        (WordTypeSortColumn.Alpha, WordSortDirection.Ascending) => "norm_text COLLATE \"C\", dimension_id",
        (WordTypeSortColumn.Alpha, WordSortDirection.Descending) => "norm_text COLLATE \"C\" DESC, dimension_id",
        // mushaf-order is ascending-only by contract (the parser rejects any suffix on it).
        (WordTypeSortColumn.MushafOrder, _) => "first_word_order_in_mushaf, dimension_id",
        _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypeSortSpec)} value."),
    };

    private static (string IdColumn, string TextColumn) DimensionColumns(WordTypeTableView view) => view switch
    {
        WordTypeTableView.Roots => ("root_id", "root_text"),
        WordTypeTableView.Stems => ("stem_id", "stem_text"),
        WordTypeTableView.Lemmas => ("lemma_id", "lemma_text"),
        _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Grouped dimension columns are only defined for roots/stems/lemmas."),
    };

    private static object[] BuildGroupedRowsParameters(WordTypeReadContext context, WordTypeSortSpec sort, int skip, int take)
    {
        var parameters = new List<object>
        {
            new NpgsqlParameter<int>("skip", skip),
            new NpgsqlParameter<int>("take", take),
        };
        AddChildCodeParameter(context, parameters);
        AddSecondaryFilterParameters(context, parameters);
        AddSearchParameter(context, parameters);

        // Same NeedsFold gate as the SQL shape — the fold pair stays parameterized, never interpolated.
        if (NeedsFold(sort))
        {
            parameters.Add(new NpgsqlParameter<string>("foldFrom", RootsListDerivation.ArabicFoldFrom));
            parameters.Add(new NpgsqlParameter<string>("foldTo", RootsListDerivation.ArabicFoldTo));
        }

        return [.. parameters];
    }

    private sealed record GroupedRowSqlResult(
        int DimensionId,
        string DisplayText,
        int OccurrencesCount,
        int AyahsCount,
        int SurahsCount,
        int FirstWordOrderInMushaf,
        // Whole-scope total from COUNT(*) OVER(); identical on every row, ignored by ToDto.
        int TotalCount)
    {
        public WordTypeTableRowDto ToDto(WordTypeTableView view) => view switch
        {
            WordTypeTableView.Roots => new RootTableRowDto(DimensionId, DisplayText, OccurrencesCount, AyahsCount, SurahsCount),
            WordTypeTableView.Stems => new StemTableRowDto(DimensionId, DisplayText, OccurrencesCount, AyahsCount, SurahsCount),
            WordTypeTableView.Lemmas => new LemmaTableRowDto(DimensionId, DisplayText, OccurrencesCount, AyahsCount, SurahsCount),
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Grouped mapping is only defined for roots/stems/lemmas."),
        };
    }
}
