using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.ResolvePhraseContextLinkingSelection;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

public sealed record PhraseSearchContextLinkingSelectionBody
{
    public string? ResolutionRef { get; init; }
    public string? PreviousRef { get; init; }
    public string? FollowingRef { get; init; }
    public string? PreviousAlternativesRef { get; init; }
    public string? FollowingAlternativesRef { get; init; }
    public string? SelectionMode { get; init; }
    public IReadOnlyList<int>? AyahIds { get; init; }
}

internal static class PhraseSearchContextLinkingSelectionBodyMapper
{
    internal static bool TryMap(
        PhraseSearchContextLinkingSelectionBody? body,
        out ResolvePhraseContextLinkingSelectionQuery query)
    {
        query = null!;
        if (body is null
            || !PhraseLinkingAyahSelectionModeParser.TryParse(body.SelectionMode, out var selectionMode)
            || !PhraseLinkingAyahSelection.TryCreate(selectionMode, body.AyahIds, out var selection))
        {
            return false;
        }

        query = new ResolvePhraseContextLinkingSelectionQuery(
            body.ResolutionRef,
            body.PreviousRef,
            body.FollowingRef,
            body.PreviousAlternativesRef,
            body.FollowingAlternativesRef,
            selection!);
        return true;
    }
}
