using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

public sealed partial class EfWordTypesReader
{
    private static string GroupedSummarySql(WordTypeReadContext context, WordTypeGroupedDimensionKind kind)
    {
        var (idColumn, textColumn) = GroupedDimensionColumns(kind);
        return $"""
        WITH base AS (
            {BaseRowsSql(context, kind)}
        )
        SELECT
            {idColumn} AS "{nameof(GroupedSummarySqlResult.DimensionId)}",
            MIN({textColumn}) AS "{nameof(GroupedSummarySqlResult.DisplayText)}",
            COUNT(*)::int AS "{nameof(GroupedSummarySqlResult.OccurrencesCount)}",
            COUNT(DISTINCT ayah_id)::int AS "{nameof(GroupedSummarySqlResult.AyahsCount)}",
            COUNT(DISTINCT surah_number)::int AS "{nameof(GroupedSummarySqlResult.SurahsCount)}"
        FROM base
        GROUP BY {idColumn}
        """;
    }

    private static (string IdColumn, string TextColumn) GroupedDimensionColumns(WordTypeGroupedDimensionKind kind) =>
        DimensionColumns(ToTableView(kind));

    private static WordTypeTableView ToTableView(WordTypeGroupedDimensionKind kind) => kind switch
    {
        WordTypeGroupedDimensionKind.Root => WordTypeTableView.Roots,
        WordTypeGroupedDimensionKind.Stem => WordTypeTableView.Stems,
        WordTypeGroupedDimensionKind.Lemma => WordTypeTableView.Lemmas,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Grouped dimension columns are only defined for root/stem/lemma."),
    };

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

    private static string GroupedAyahsCountSql(WordTypeReadContext context, WordTypeGroupedDimensionKind kind) => $"""
        WITH base AS (
            {BaseRowsSql(context, kind)}
        )
        SELECT COUNT(DISTINCT ayah_id)::int AS "{nameof(CountRow.Count)}"
        FROM base
        """;

    private static string GroupedAyahsPageSql(WordTypeReadContext context, WordTypeGroupedDimensionKind kind) => $"""
        WITH base AS (
            {BaseRowsSql(context, kind)}
        ), page_ayahs AS (
            SELECT ayah_id, surah_number, ayah_number
            FROM base
            GROUP BY ayah_id, surah_number, ayah_number
            ORDER BY surah_number, ayah_number
            OFFSET @skip LIMIT @take
        )
        SELECT
            pa.ayah_id AS "{nameof(GroupedAyahMatchSqlRow.AyahId)}",
            a.verse_key AS "{nameof(GroupedAyahMatchSqlRow.VerseKey)}",
            pa.surah_number::int AS "{nameof(GroupedAyahMatchSqlRow.SurahNumber)}",
            pa.ayah_number::int AS "{nameof(GroupedAyahMatchSqlRow.AyahNumber)}",
            b.quran_word_id AS "{nameof(GroupedAyahMatchSqlRow.MatchedWordId)}",
            b.word_number AS "{nameof(GroupedAyahMatchSqlRow.MatchedWordNumber)}"
        FROM page_ayahs pa
        JOIN quran_ayahs a ON a.id = pa.ayah_id
        JOIN base b ON b.ayah_id = pa.ayah_id
        ORDER BY pa.surah_number, pa.ayah_number, b.word_number, b.quran_word_id
        """;

    private static string GroupedSurahsSql(WordTypeReadContext context, WordTypeGroupedDimensionKind kind) => $"""
        WITH base AS (
            {BaseRowsSql(context, kind)}
        )
        SELECT
            surah_number::int AS "{nameof(GroupedSurahOccurrenceRow.SurahNumber)}",
            COUNT(*)::int AS "{nameof(GroupedSurahOccurrenceRow.OccurrencesCount)}"
        FROM base
        GROUP BY surah_number
        ORDER BY surah_number
        """;

    private sealed record GroupedSurahOccurrenceRow(int SurahNumber, int OccurrencesCount);

    private sealed record GroupedAyahMatchSqlRow(
        int AyahId,
        string VerseKey,
        int SurahNumber,
        int AyahNumber,
        int MatchedWordId,
        int MatchedWordNumber);

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
