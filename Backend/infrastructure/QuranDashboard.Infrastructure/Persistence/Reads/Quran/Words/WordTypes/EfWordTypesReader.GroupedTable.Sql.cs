using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

public sealed partial class EfWordTypesReader
{
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

    private static bool NeedsFold(WordTypeSortSpec sort) => sort.Column == WordTypeSortColumn.Alpha;

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

        // Fold pair stays parameterized, never interpolated (injection).
        if (NeedsFold(sort))
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
        public WordTypeTableRowDto ToDto(WordTypeTableView view) => view switch
        {
            WordTypeTableView.Roots => new RootTableRowDto(DimensionId, DisplayText, OccurrencesCount, AyahsCount, SurahsCount),
            WordTypeTableView.Stems => new StemTableRowDto(DimensionId, DisplayText, OccurrencesCount, AyahsCount, SurahsCount),
            WordTypeTableView.Lemmas => new LemmaTableRowDto(DimensionId, DisplayText, OccurrencesCount, AyahsCount, SurahsCount),
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Grouped mapping is only defined for roots/stems/lemmas."),
        };
    }
}
