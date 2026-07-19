using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

public sealed partial class EfWordTypesReader
{
    public async Task<WordTypeScopeCountsDto> GetScopeCountsAsync(
        WordTypeFilter filter,
        CancellationToken cancellationToken)
    {
        var type = NormalizeType(filter.Type);
        var childCode = NormalizeChildCode(filter.ChildCode);
        var context = new WordTypeReadContext(
            type,
            childCode,
            filter.Case,
            filter.Tense,
            filter.Voice,
            ArabicSearchQueryNormalizer.Normalize(filter.Search),
            filter.HasRoot,
            filter.HasStem,
            filter.HasLemma);

        // No paging: BuildCountParameters binds exactly the base predicates' parameters (childCode,
        // secondary filters, search) and nothing else, so the single command stays in sync.
        var parameters = BuildCountParameters(context);
        var result = await _dbContext.Database
            .SqlQueryRaw<ScopeCountsRow>(ScopeCountsSql(context), parameters)
            .SingleAsync(cancellationToken);

        return new WordTypeScopeCountsDto(
            result.WordsCount,
            result.RootsCount,
            result.StemsCount,
            result.LemmasCount);
    }

    // Reuses BaseRowsSql (scoped base, search + flags) and ContextExpression (the words-view context grain)
    // rather than re-deriving them. words = COUNT(DISTINCT (tashkeel_word_id, context_code)) is the row
    // constructor form of RowsCountSql's GROUP BY (tashkeel_word_id, context_code) + COUNT(*); the three
    // dimension counts are COUNT(DISTINCT <dim>_id), the GroupedRowsCountSql formula (NULLs excluded).
    private static string ScopeCountsSql(WordTypeReadContext context) => $"""
        WITH base AS (
            {BaseRowsSql(context)}
        )
        SELECT
            COUNT(DISTINCT (tashkeel_word_id, {ContextExpression(context)}))::int AS "{nameof(ScopeCountsRow.WordsCount)}",
            COUNT(DISTINCT root_id)::int AS "{nameof(ScopeCountsRow.RootsCount)}",
            COUNT(DISTINCT stem_id)::int AS "{nameof(ScopeCountsRow.StemsCount)}",
            COUNT(DISTINCT lemma_id)::int AS "{nameof(ScopeCountsRow.LemmasCount)}"
        FROM base
        """;

    private sealed record ScopeCountsRow(int WordsCount, int RootsCount, int StemsCount, int LemmasCount);
}
