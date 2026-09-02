using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.ResolvePhraseSimilarityLinkingSelection;

public sealed class ResolvePhraseSimilarityLinkingSelectionHandler(
    IPhraseSimilarityReader reader,
    IPhraseSearchReferenceCodec codec)
{
    public async Task<PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>> HandleAsync(
        ResolvePhraseSimilarityLinkingSelectionQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!codec.TryDecodeResolution(query.ResolutionRef, out var resolution)
            || resolution is null)
        {
            return new PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>.Invalid(
                PhraseRequestInvalidKind.Reference);
        }

        if (!PhraseSimilarityRequestValidation.TryMinimumMatchedWords(
                resolution,
                query.MinimumMatchedWords,
                out var minimumMatchedWords,
                out var invalidKind))
        {
            return new PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>.Invalid(invalidKind);
        }

        var result = await reader.GetLinkingSelectionAsync(
            resolution,
            minimumMatchedWords,
            query.Selection,
            cancellationToken);
        return result switch
        {
            PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>.Success success =>
                new PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>.Success(success.Value),
            PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>.Unavailable =>
                new PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>.Unavailable(),
            PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>.BuildChanged =>
                new PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>.BuildChanged(),
            PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>.InvalidReference =>
                new PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>.Invalid(
                    PhraseRequestInvalidKind.Reference),
            PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>.InvalidSelection =>
                new PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>.Invalid(
                    PhraseRequestInvalidKind.Selection),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(PhraseSearchReadResult<PhraseSimilarityLinkingSelectionResponse>)} variant."),
        };
    }
}
