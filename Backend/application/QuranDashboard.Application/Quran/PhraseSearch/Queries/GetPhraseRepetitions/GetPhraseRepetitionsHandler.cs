using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseRepetitions;

public sealed class GetPhraseRepetitionsHandler(IPhraseRepetitionsReader reader)
{
    public async Task<GetPhraseRepetitionsOutcome> HandleAsync(
        GetPhraseRepetitionsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var modeValue = string.IsNullOrWhiteSpace(query.Mode)
            ? PhraseTextModeKeys.Simple
            : query.Mode;
        if (!PhraseTextModeContract.TryParse(modeValue, out var mode))
        {
            return new GetPhraseRepetitionsOutcome.InvalidMode();
        }

        var wordCount = query.WordCount ?? PhraseSearchPaging.MinimumRepetitionLength;
        if (wordCount is < PhraseSearchPaging.MinimumRepetitionLength
            or > PhraseSearchPaging.MaximumSourceLength)
        {
            return new GetPhraseRepetitionsOutcome.InvalidLength();
        }

        var sortValue = string.IsNullOrWhiteSpace(query.Sort)
            ? PhraseRepetitionSortKeys.Occurrences
            : query.Sort;
        if (!PhraseRepetitionSortContract.TryParse(sortValue, out var sort))
        {
            return new GetPhraseRepetitionsOutcome.InvalidSort();
        }

        var page = query.Page ?? PhraseSearchPaging.DefaultPage;
        var pageSize = query.PageSize ?? PhraseSearchPaging.DefaultPageSize;
        if (!IsPagingValid(page, pageSize))
        {
            return new GetPhraseRepetitionsOutcome.InvalidPaging();
        }

        var result = await reader.GetRepetitionsAsync(
            mode,
            checked((short)wordCount),
            sort,
            page,
            pageSize,
            cancellationToken);

        return result switch
        {
            PhraseSearchReadResult<PhraseRepetitionsPageResponse>.Success success =>
                new GetPhraseRepetitionsOutcome.Success(success.Value),
            PhraseSearchReadResult<PhraseRepetitionsPageResponse>.Unavailable =>
                new GetPhraseRepetitionsOutcome.Unavailable(),
            _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseSearchReadResult<PhraseRepetitionsPageResponse>)} variant."),
        };
    }

    private static bool IsPagingValid(int page, int pageSize) =>
        page >= PhraseSearchPaging.DefaultPage
        && pageSize > 0
        && pageSize <= PhraseSearchPaging.MaximumPageSize;
}
