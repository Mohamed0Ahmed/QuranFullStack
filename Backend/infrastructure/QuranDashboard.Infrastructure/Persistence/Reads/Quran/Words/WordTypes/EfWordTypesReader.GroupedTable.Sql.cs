using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

public sealed partial class EfWordTypesReader
{
    private static string GroupedRowsSql(WordTypeReadContext context, WordTypeTableView view, string sortToken)
    {
        var (idColumn, textColumn) = DimensionColumns(view.Key);
        var normTextColumn = NeedsFold(sortToken)
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
        ORDER BY {GroupedOrderBy(sortToken)}
        OFFSET @skip LIMIT @take
        """;
    }

    // THE single fold predicate. The grouped alpha ORDER BY reads the folded `norm_text` column, which
    // only exists when GroupedRowsSql projects it AND @foldFrom/@foldTo are bound. Both sites gate on
    // this one method: if they ever disagree, the query either sorts on a missing column or Npgsql
    // rejects an unbound parameter — at RUNTIME. Alpha in EITHER direction folds.
    private static bool NeedsFold(string sortToken) => sortToken is "alpha" or "alpha-desc";

    private static string GroupedRowsCountSql(WordTypeReadContext context, WordTypeTableView view)
    {
        var (idColumn, _) = DimensionColumns(view.Key);
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
    private static string GroupedOrderBy(string sortToken) => sortToken switch
    {
        "occurrences" => "occurrences_count DESC, first_word_order_in_mushaf, dimension_id",
        "occurrences-asc" => "occurrences_count, first_word_order_in_mushaf, dimension_id",
        "ayahs" => "ayahs_count DESC, first_word_order_in_mushaf, dimension_id",
        "ayahs-asc" => "ayahs_count, first_word_order_in_mushaf, dimension_id",
        "surahs" => "surahs_count DESC, first_word_order_in_mushaf, dimension_id",
        "surahs-asc" => "surahs_count, first_word_order_in_mushaf, dimension_id",
        "alpha" => "norm_text COLLATE \"C\", dimension_id",
        "alpha-desc" => "norm_text COLLATE \"C\" DESC, dimension_id",
        // mushaf-order is ascending-only by contract (the parser rejects any suffix on it).
        "mushaf-order" => "first_word_order_in_mushaf, dimension_id",
        _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypeSortSpec)} value."),
    };

    private static (string IdColumn, string TextColumn) DimensionColumns(string viewKey) => viewKey switch
    {
        "roots" => ("root_id", "root_text"),
        "stems" => ("stem_id", "stem_text"),
        "lemmas" => ("lemma_id", "lemma_text"),
        _ => throw new ArgumentOutOfRangeException(nameof(viewKey), viewKey, "Grouped dimension columns are only defined for roots/stems/lemmas."),
    };

    private static object[] BuildGroupedRowsParameters(WordTypeReadContext context, string sortToken, int skip, int take)
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
        if (NeedsFold(sortToken))
        {
            parameters.Add(new NpgsqlParameter<string>("foldFrom", ArabicSearchQueryNormalizer.FoldFrom));
            parameters.Add(new NpgsqlParameter<string>("foldTo", ArabicSearchQueryNormalizer.FoldTo));
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
        int TotalCount)
    {
        public WordTypeTableRowDto ToDto(WordTypeTableView view) => view.Key switch
        {
            "roots" => new RootTableRowDto(DimensionId, DisplayText, OccurrencesCount, AyahsCount, SurahsCount),
            "stems" => new StemTableRowDto(DimensionId, DisplayText, OccurrencesCount, AyahsCount, SurahsCount),
            "lemmas" => new LemmaTableRowDto(DimensionId, DisplayText, OccurrencesCount, AyahsCount, SurahsCount),
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Grouped mapping is only defined for roots/stems/lemmas."),
        };
    }
}
