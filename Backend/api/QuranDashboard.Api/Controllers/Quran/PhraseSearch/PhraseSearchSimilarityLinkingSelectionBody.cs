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
        if (body?.AyahIds is null || !TryParseSelectionMode(body.SelectionMode, out var selectionMode))
        {
            return false;
        }

        query = new ResolvePhraseSimilarityLinkingSelectionQuery(
            body.ResolutionRef,
            body.MinimumMatchedWords,
            selectionMode,
            body.AyahIds);
        return true;
    }

    private static bool TryParseSelectionMode(
        string? value,
        out PhraseSimilarityAyahSelectionMode selectionMode)
    {
        selectionMode = default;
        if (string.Equals(value, "only", StringComparison.Ordinal))
        {
            selectionMode = PhraseSimilarityAyahSelectionMode.Only;
            return true;
        }

        if (string.Equals(value, "all-except", StringComparison.Ordinal))
        {
            selectionMode = PhraseSimilarityAyahSelectionMode.AllExcept;
            return true;
        }

        return false;
    }
}
