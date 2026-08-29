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
        if (query.SelectionMode is null
            || !Enum.IsDefined(query.SelectionMode.Value)
            || query.AyahIds is null
            || query.AyahIds.Any(ayahId => ayahId <= 0)
            || query.AyahIds.Distinct().Count() != query.AyahIds.Count)
        {
            return new PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>.Invalid(
                PhraseRequestInvalidKind.Selection);
        }

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
            new PhraseSimilarityLinkingSelection(query.SelectionMode.Value, query.AyahIds),
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
