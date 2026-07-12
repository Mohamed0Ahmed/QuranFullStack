using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

// Grouped (root/stem/lemma) scoped detail reads. Kept in a dedicated partial so the primary reader
// stays under its size threshold. Every read reuses the same scoped BaseRowsSql occurrence base as the
// word/table rows and restricts to a single numeric dimension ID at head-word grain.
public sealed partial class EfWordTypesReader
{
    public async Task<WordTypeGroupedSummaryDto?> GetGroupedSummaryAsync(
        WordTypeGroupedSelection selection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if (!selection.IsValid)
        {
            return null;
        }

        var context = ToGroupedReadContext(selection.Filter);
        var parameters = BuildGroupedDetailParameters(context, selection.DimensionId);

        var row = await _dbContext.Database
            .SqlQueryRaw<GroupedSummarySqlResult>(GroupedSummarySql(context, selection.Kind), parameters)
            .SingleOrDefaultAsync(cancellationToken);

        return row?.ToDto(selection.Kind);
    }

    public async Task<PagedResult<WordTypeGroupedMemberWordDto>?> GetGroupedMemberWordsAsync(
        WordTypeGroupedSelection selection,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if (!selection.IsValid)
        {
            return null;
        }

        var context = ToGroupedReadContext(selection.Filter);

        // A dimension absent from the scope has zero grouped word-context rows → not found (null). An
        // existing dimension with an out-of-range page falls through to a non-null empty page below.
        var totalCount = await CountGroupedMemberWordsAsync(context, selection.Kind, selection.DimensionId, cancellationToken);
        if (totalCount == 0)
        {
            return null;
        }

        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<WordTypeGroupedMemberWordDto>(page, pageSize, totalCount, []);
        }

        var parameters = BuildRowsParameters(context, skip.Value, pageSize, selection.DimensionId);
        var rows = await _dbContext.Database
            .SqlQueryRaw<WordTypeRowSqlResult>(RowsSql(context, WordTypeSort.Occurrences, selection.Kind), parameters)
            .ToListAsync(cancellationToken);

        return new PagedResult<WordTypeGroupedMemberWordDto>(
            page,
            pageSize,
            totalCount,
            rows.Select(row => ToGroupedMemberWordDto(row, selection.Filter)).ToList());
    }

    private async Task<int> CountGroupedMemberWordsAsync(
        WordTypeReadContext context,
        WordTypeGroupedDimensionKind kind,
        int dimensionId,
        CancellationToken cancellationToken)
    {
        var parameters = BuildCountParameters(context, dimensionId);
        var result = await _dbContext.Database
            .SqlQueryRaw<CountRow>(RowsCountSql(context, kind), parameters)
            .SingleAsync(cancellationToken);

        return result.Count;
    }

    // Member rows carry the active Case/Tense/Voice scope exactly as the Words table row does; the
    // root/lemma/stem text stay projection-only display values.
    private static WordTypeGroupedMemberWordDto ToGroupedMemberWordDto(WordTypeRowSqlResult row, WordTypeFilter filter) => new(
        row.TashkeelWordId,
        row.ContextCode,
        filter.Case,
        filter.Tense,
        filter.Voice,
        row.DisplayText,
        row.TypeCode,
        new WordTypeLabelDto(row.TypeLabel),
        new WordTypeLabelDto(row.BroadLabel),
        row.CaseOrFeature,
        row.RootText,
        row.LemmaText,
        row.StemText,
        row.OccurrencesCount,
        row.AyahsCount,
        row.SurahsCount);
}
