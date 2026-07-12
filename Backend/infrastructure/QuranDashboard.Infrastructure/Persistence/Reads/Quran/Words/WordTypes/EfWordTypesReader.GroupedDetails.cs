using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
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

    public async Task<PagedResult<WordTypeAyahMatchDto>?> GetGroupedAyahMatchesAsync(
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

        // The distinct-ayah count doubles as the existence check: zero scoped ayahs → dimension absent → null.
        var countParameters = BuildCountParameters(context, selection.DimensionId);
        var totalCount = (await _dbContext.Database
            .SqlQueryRaw<CountRow>(GroupedAyahsCountSql(context, selection.Kind), countParameters)
            .SingleAsync(cancellationToken)).Count;
        if (totalCount == 0)
        {
            return null;
        }

        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<WordTypeAyahMatchDto>(page, pageSize, totalCount, []);
        }

        // One grouped-page query returns the distinct page ayahs joined to their scoped matched words.
        var pageParameters = BuildRowsParameters(context, skip.Value, pageSize, selection.DimensionId);
        var matchRows = await _dbContext.Database
            .SqlQueryRaw<GroupedAyahMatchSqlRow>(GroupedAyahsPageSql(context, selection.Kind), pageParameters)
            .ToListAsync(cancellationToken);

        // Preserve the SQL's Mushaf order while collapsing the one-row-per-matched-word shape into ayahs.
        var pageAyahs = matchRows
            .GroupBy(row => row.AyahId)
            .Select(group => group.First())
            .ToList();
        var matchesByAyah = matchRows
            .GroupBy(row => row.AyahId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var ayahIds = pageAyahs.Select(ayah => ayah.AyahId).ToList();

        // One bounded hydration query loads every readable word (canonical Uthmani text) for the page ayahs.
        var wordsByAyah = await _dbContext.QuranWords
            .AsNoTracking()
            .Where(word => ayahIds.Contains(word.AyahId) && !word.IsAyahMarker)
            .OrderBy(word => word.SurahNumber)
            .ThenBy(word => word.AyahNumber)
            .ThenBy(word => word.WordNumber)
            .Select(word => new AyahWordRow(
                word.AyahId,
                word.Id,
                word.WordNumber,
                word.PageNumber,
                word.TextUthmani,
                word.IsAyahMarker))
            .ToListAsync(cancellationToken);

        var wordsGrouped = wordsByAyah
            .GroupBy(word => word.AyahId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var items = pageAyahs
            .Select(ayah =>
            {
                var words = wordsGrouped.GetValueOrDefault(ayah.AyahId, []);
                var matched = matchesByAyah.GetValueOrDefault(ayah.AyahId, []);
                var matchedPositions = matched.Select(row => row.MatchedWordNumber).Distinct().OrderBy(number => number).ToList();

                return new WordTypeAyahMatchDto(
                    ayah.VerseKey,
                    ayah.SurahNumber,
                    ayah.AyahNumber,
                    ResolveAyahPageNumber(words),
                    matchedPositions,
                    matched.Select(row => row.MatchedWordId).Distinct().ToList(),
                    words.Select(word => new AyahWordForHighlightDto(
                        word.QuranWordId,
                        word.TextUthmani,
                        word.IsAyahMarker)).ToList());
            })
            .ToList();

        return new PagedResult<WordTypeAyahMatchDto>(page, pageSize, totalCount, items);
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
