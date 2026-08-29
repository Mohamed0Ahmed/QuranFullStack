using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.ResolvePhraseContextLinkingSelection;

public sealed class ResolvePhraseContextLinkingSelectionHandler(
    IPhraseContextReader reader,
    PhraseContextRequestParser parser)
{
    public async Task<PhraseReadOutcome<PhraseContextLinkingSelectionResponse>> HandleAsync(
        ResolvePhraseContextLinkingSelectionQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.SelectionMode is null
            || !Enum.IsDefined(query.SelectionMode.Value)
            || query.AyahIds is null
            || query.AyahIds.Any(ayahId => ayahId <= 0)
            || query.AyahIds.Distinct().Count() != query.AyahIds.Count)
        {
            return new PhraseReadOutcome<PhraseContextLinkingSelectionResponse>.Invalid(
                PhraseRequestInvalidKind.Selection);
        }

        if (!parser.TryParseSelection(
                query.Resolution,
                query.Previous,
                query.Following,
                query.PreviousAlternatives,
                query.FollowingAlternatives,
                out var selection)
            || selection is null)
        {
            return new PhraseReadOutcome<PhraseContextLinkingSelectionResponse>.Invalid(
                PhraseRequestInvalidKind.Reference);
        }

        var result = await reader.GetLinkingSelectionAsync(
            selection,
            new PhraseContextLinkingSelection(query.SelectionMode.Value, query.AyahIds),
            cancellationToken);
        return result switch
        {
            PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.Success success =>
                new PhraseReadOutcome<PhraseContextLinkingSelectionResponse>.Success(success.Value),
            PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.Unavailable =>
                new PhraseReadOutcome<PhraseContextLinkingSelectionResponse>.Unavailable(),
            PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.BuildChanged =>
                new PhraseReadOutcome<PhraseContextLinkingSelectionResponse>.BuildChanged(),
            PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.InvalidReference =>
                new PhraseReadOutcome<PhraseContextLinkingSelectionResponse>.Invalid(
                    PhraseRequestInvalidKind.Reference),
            PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.InvalidSelection =>
                new PhraseReadOutcome<PhraseContextLinkingSelectionResponse>.Invalid(
                    PhraseRequestInvalidKind.Selection),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>)} variant."),
        };
    }
}
