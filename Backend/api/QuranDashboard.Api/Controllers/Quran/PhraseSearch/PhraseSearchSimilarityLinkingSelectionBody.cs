using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.ResolvePhraseSimilarityLinkingSelection;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

public sealed record PhraseSearchSimilarityLinkingSelectionBody
{
    public string? ResolutionRef { get; init; }
    public int? MinimumMatchedWords { get; init; }
    public string? SelectionMode { get; init; }
    public IReadOnlyList<int>? AyahIds { get; init; }
}

internal static class PhraseSearchSimilarityLinkingSelectionBodyMapper
{
    internal static bool TryMap(
        PhraseSearchSimilarityLinkingSelectionBody? body,
        out ResolvePhraseSimilarityLinkingSelectionQuery query)
    {
        query = null!;
        if (body is null
            || !PhraseLinkingAyahSelectionModeParser.TryParse(body.SelectionMode, out var selectionMode)
            || !PhraseLinkingAyahSelection.TryCreate(selectionMode, body.AyahIds, out var selection))
        {
            return false;
        }

        query = new ResolvePhraseSimilarityLinkingSelectionQuery(
            body.ResolutionRef,
            body.MinimumMatchedWords,
            selection!);
        return true;
    }
}
