using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

public sealed partial class EfWordTypesReader
{
    // Scoped grouped summary: the same BaseRowsSql occurrence base, restricted to one numeric dimension
    // ID, aggregated into the three distinct measures. head-level quran_word_morphology only — the
    // segments table is deliberately never referenced.
    private static string GroupedSummarySql(WordTypeReadContext context, WordTypeGroupedDimensionKind kind)
    {
        var (idColumn, textColumn) = GroupedDimensionColumns(kind);
        return $"""
        WITH base AS (
            {BaseRowsSql(context)}
        )
        SELECT
            {idColumn} AS "{nameof(GroupedSummarySqlResult.DimensionId)}",
            MIN({textColumn}) AS "{nameof(GroupedSummarySqlResult.DisplayText)}",
            COUNT(*)::int AS "{nameof(GroupedSummarySqlResult.OccurrencesCount)}",
            COUNT(DISTINCT ayah_id)::int AS "{nameof(GroupedSummarySqlResult.AyahsCount)}",
            COUNT(DISTINCT surah_number)::int AS "{nameof(GroupedSummarySqlResult.SurahsCount)}"
        FROM base
        WHERE {idColumn} = @dimensionId
        GROUP BY {idColumn}
        """;
    }

    // Allowlisted numeric membership columns. The text columns are projection-only display fields and
    // never participate in the membership predicate.
    private static (string IdColumn, string TextColumn) GroupedDimensionColumns(WordTypeGroupedDimensionKind kind) => kind switch
    {
        WordTypeGroupedDimensionKind.Root => ("root_id", "root_text"),
        WordTypeGroupedDimensionKind.Stem => ("stem_id", "stem_text"),
        WordTypeGroupedDimensionKind.Lemma => ("lemma_id", "lemma_text"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Grouped dimension columns are only defined for root/stem/lemma."),
    };

    // Grouped detail reads apply the identical five-field scope as the table row, so they build the
    // same WordTypeReadContext the list SQL uses.
    private static WordTypeReadContext ToGroupedReadContext(WordTypeFilter filter) =>
        new(NormalizeType(filter.Type), NormalizeChildCode(filter.ChildCode), filter.Case, filter.Tense, filter.Voice);

    private static object[] BuildGroupedDetailParameters(WordTypeReadContext context, int dimensionId)
    {
        var parameters = new List<object>
        {
            new NpgsqlParameter<int>("dimensionId", dimensionId),
        };
        AddChildCodeParameter(context, parameters);
        AddSecondaryFilterParameters(context, parameters);
        return [.. parameters];
    }

    private sealed record GroupedSummarySqlResult(
        int DimensionId,
        string DisplayText,
        int OccurrencesCount,
        int AyahsCount,
        int SurahsCount)
    {
        public WordTypeGroupedSummaryDto ToDto(WordTypeGroupedDimensionKind kind) => new(
            kind.ToDtoKind(),
            DimensionId,
            DisplayText,
            OccurrencesCount,
            AyahsCount,
            SurahsCount);
    }
}
