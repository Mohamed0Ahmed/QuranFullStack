using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.SearchPhraseSimilarities;

public sealed class SearchPhraseSimilaritiesHandler(
    IPhraseSimilarityReader reader,
    IPhraseSearchReferenceCodec codec)
{
    public async Task<PhraseReadOutcome<PhraseSimilaritySearchResponse>> HandleAsync(
        SearchPhraseSimilaritiesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!codec.TryDecodeResolution(query.ResolutionRef, out var resolution)
            || resolution is null)
        {
            return new PhraseReadOutcome<PhraseSimilaritySearchResponse>.Invalid(
                PhraseRequestInvalidKind.Reference);
        }

        if (!PhraseSimilarityRequestValidation.TryMinimumMatchedWords(
                resolution,
                query.MinimumMatchedWords,
                out var minimumMatchedWords,
                out var invalidKind))
        {
            return new PhraseReadOutcome<PhraseSimilaritySearchResponse>.Invalid(
                invalidKind);
        }

        var requestedSort = query.Sort ?? PhraseSimilaritySortKeys.Strength;
        if (!PhraseSimilaritySortContract.TryParse(requestedSort, out var sort)
            || sort == PhraseSimilaritySort.Connections)
        {
            return new PhraseReadOutcome<PhraseSimilaritySearchResponse>.Invalid(
                PhraseRequestInvalidKind.Sort);
        }

        if (!PhraseSimilarityRequestValidation.TryPaging(
                query.Page,
                query.PageSize,
                out var page,
                out var pageSize))
        {
            return new PhraseReadOutcome<PhraseSimilaritySearchResponse>.Invalid(
                PhraseRequestInvalidKind.Paging);
        }

        var result = await reader.SearchAsync(
            resolution,
            minimumMatchedWords,
            sort,
            page,
            pageSize,
            cancellationToken);
        return result switch
        {
            PhraseSearchReadResult<PhraseSimilaritySearchResponse>.Success success =>
                new PhraseReadOutcome<PhraseSimilaritySearchResponse>.Success(success.Value),
            PhraseSearchReadResult<PhraseSimilaritySearchResponse>.Unavailable =>
                new PhraseReadOutcome<PhraseSimilaritySearchResponse>.Unavailable(),
            PhraseSearchReadResult<PhraseSimilaritySearchResponse>.BuildChanged =>
                new PhraseReadOutcome<PhraseSimilaritySearchResponse>.BuildChanged(),
            PhraseSearchReadResult<PhraseSimilaritySearchResponse>.InvalidReference =>
                new PhraseReadOutcome<PhraseSimilaritySearchResponse>.Invalid(
                    PhraseRequestInvalidKind.Reference),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(PhraseSearchReadResult<PhraseSimilaritySearchResponse>)} variant."),
        };
    }
}
