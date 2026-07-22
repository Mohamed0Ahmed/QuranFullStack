using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

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
            .SqlQueryRaw<WordTypeRowSqlResult>(RowsSql(context, WordTypeSortSpec.Natural(WordTypeSortColumn.Occurrences), selection.Kind), parameters)
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

        var pageParameters = BuildRowsParameters(context, skip.Value, pageSize, selection.DimensionId);
        var matchRows = await _dbContext.Database
            .SqlQueryRaw<GroupedAyahMatchSqlRow>(GroupedAyahsPageSql(context, selection.Kind), pageParameters)
            .ToListAsync(cancellationToken);

        var pageAyahs = matchRows
            .GroupBy(row => row.AyahId)
            .Select(group => group.First())
            .ToList();
        var matchesByAyah = matchRows
            .GroupBy(row => row.AyahId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var items = await AyahWordHydration.ProjectAyahMatchesAsync(
            _dbContext,
            pageAyahs,
            ayah => ayah.AyahId,
            (ayah, words, pageNumber) =>
            {
                var matched = matchesByAyah.GetValueOrDefault(ayah.AyahId, []);
                var matchedPositions = matched.Select(row => row.MatchedWordNumber).Distinct().OrderBy(number => number).ToList();

                return new WordTypeAyahMatchDto(
                    ayah.VerseKey,
                    ayah.SurahNumber,
                    ayah.AyahNumber,
                    pageNumber,
                    matchedPositions,
                    matched.Select(row => row.MatchedWordId).Distinct().ToList(),
                    words.Select(word => new AyahWordForHighlightDto(
                        word.QuranWordId,
                        word.TextUthmani,
                        word.IsAyahMarker)).ToList());
            },
            cancellationToken);

        return new PagedResult<WordTypeAyahMatchDto>(page, pageSize, totalCount, items);
    }

    public async Task<WordTypeSurahsResponse?> GetGroupedSurahsAsync(
        WordTypeGroupedSelection selection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if (!selection.IsValid)
        {
            return null;
        }

        var context = ToGroupedReadContext(selection.Filter);
        var parameters = BuildCountParameters(context, selection.DimensionId);

        var occurrences = await _dbContext.Database
            .SqlQueryRaw<GroupedSurahOccurrenceRow>(GroupedSurahsSql(context, selection.Kind), parameters)
            .ToListAsync(cancellationToken);
        if (occurrences.Count == 0)
        {
            return null;
        }

        var occurrencesByNumber = occurrences.ToDictionary(row => row.SurahNumber, row => row.OccurrencesCount);

        var catalogue = await _dbContext.QuranSurahs
            .AsNoTracking()
            .OrderBy(surah => surah.SurahNumber)
            .Select(surah => new SurahCatalogueRow((int)surah.SurahNumber, surah.NameArabic))
            .ToListAsync(cancellationToken);

        var surahs = new List<WordTypeSurahOccurrenceDto>();
        var missingSurahs = new List<WordTypeMissingSurahDto>();
        foreach (var surah in catalogue)
        {
            if (occurrencesByNumber.TryGetValue(surah.SurahNumber, out var occurrencesCount))
            {
                surahs.Add(new WordTypeSurahOccurrenceDto(surah.SurahNumber, surah.NameArabic, occurrencesCount));
            }
            else
            {
                missingSurahs.Add(new WordTypeMissingSurahDto(surah.SurahNumber, surah.NameArabic));
            }
        }

        return new WordTypeSurahsResponse(surahs, missingSurahs);
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

    private sealed record SurahCatalogueRow(int SurahNumber, string NameArabic);
}
