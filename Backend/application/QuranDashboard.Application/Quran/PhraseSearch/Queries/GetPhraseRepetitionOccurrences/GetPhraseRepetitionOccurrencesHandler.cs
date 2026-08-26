using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseRepetitionOccurrences;

public sealed class GetPhraseRepetitionOccurrencesHandler(IPhraseRepetitionsReader reader)
{
    public async Task<GetPhraseRepetitionOccurrencesOutcome> HandleAsync(
        GetPhraseRepetitionOccurrencesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.BuildId == Guid.Empty || query.VariantId <= 0)
        {
            return new GetPhraseRepetitionOccurrencesOutcome.InvalidReference();
        }

        var page = query.Page ?? PhraseSearchPaging.DefaultPage;
        var pageSize = query.PageSize ?? PhraseSearchPaging.DefaultPageSize;
        if (page < PhraseSearchPaging.DefaultPage
            || pageSize <= 0
            || pageSize > PhraseSearchPaging.MaximumPageSize)
        {
            return new GetPhraseRepetitionOccurrencesOutcome.InvalidPaging();
        }

        var result = await reader.GetOccurrencesAsync(
            query.BuildId,
            query.VariantId,
            page,
            pageSize,
            cancellationToken);

        return result switch
        {
            PhraseSearchReadResult<PhraseOccurrencePageResponse>.Success success =>
                new GetPhraseRepetitionOccurrencesOutcome.Success(success.Value),
            PhraseSearchReadResult<PhraseOccurrencePageResponse>.Unavailable =>
                new GetPhraseRepetitionOccurrencesOutcome.Unavailable(),
            PhraseSearchReadResult<PhraseOccurrencePageResponse>.BuildChanged =>
                new GetPhraseRepetitionOccurrencesOutcome.BuildChanged(),
            PhraseSearchReadResult<PhraseOccurrencePageResponse>.NotFound =>
                new GetPhraseRepetitionOccurrencesOutcome.NotFound(),
            _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseSearchReadResult<PhraseOccurrencePageResponse>)} variant."),
        };
    }
}
