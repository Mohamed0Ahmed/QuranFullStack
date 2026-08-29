using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSimilarityMatches;

public sealed class GetPhraseSimilarityMatchesHandler(IPhraseSimilarityReader reader)
{
    public async Task<PhraseReadOutcome<PhraseSimilarityMatchesResponse>> HandleAsync(
        GetPhraseSimilarityMatchesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.BuildId == Guid.Empty || query.VariantId <= 0)
        {
            return new PhraseReadOutcome<PhraseSimilarityMatchesResponse>.Invalid(
                PhraseRequestInvalidKind.Reference);
        }

        var threshold = query.Threshold ?? PhraseSimilarityContract.DefaultThreshold;
        if (!PhraseSimilarityContract.IsPresetThreshold(threshold))
        {
            return new PhraseReadOutcome<PhraseSimilarityMatchesResponse>.Invalid(
                PhraseRequestInvalidKind.Threshold);
        }

        if (!PhraseSimilarityRequestValidation.TryPaging(
                query.Page,
                query.PageSize,
                out var page,
                out var pageSize))
        {
            return new PhraseReadOutcome<PhraseSimilarityMatchesResponse>.Invalid(
                PhraseRequestInvalidKind.Paging);
        }

        var result = await reader.GetMatchesAsync(
            query.BuildId,
            query.VariantId,
            checked((short)threshold),
            page,
            pageSize,
            cancellationToken);
        return result switch
        {
            PhraseSearchReadResult<PhraseSimilarityMatchesResponse>.Success success =>
                new PhraseReadOutcome<PhraseSimilarityMatchesResponse>.Success(success.Value),
            PhraseSearchReadResult<PhraseSimilarityMatchesResponse>.Unavailable =>
                new PhraseReadOutcome<PhraseSimilarityMatchesResponse>.Unavailable(),
            PhraseSearchReadResult<PhraseSimilarityMatchesResponse>.BuildChanged =>
                new PhraseReadOutcome<PhraseSimilarityMatchesResponse>.BuildChanged(),
            PhraseSearchReadResult<PhraseSimilarityMatchesResponse>.NotFound =>
                new PhraseReadOutcome<PhraseSimilarityMatchesResponse>.NotFound(),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(PhraseSearchReadResult<PhraseSimilarityMatchesResponse>)} variant."),
        };
    }
}
