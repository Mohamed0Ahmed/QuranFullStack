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
        if (body?.AyahIds is null || !TryParseSelectionMode(body.SelectionMode, out var selectionMode))
        {
            return false;
        }

        query = new ResolvePhraseContextLinkingSelectionQuery(
            body.ResolutionRef,
            body.PreviousRef,
            body.FollowingRef,
            body.PreviousAlternativesRef,
            body.FollowingAlternativesRef,
            selectionMode,
            body.AyahIds);
        return true;
    }

    private static bool TryParseSelectionMode(
        string? value,
        out PhraseContextAyahSelectionMode selectionMode)
    {
        selectionMode = default;
        if (string.Equals(value, "only", StringComparison.Ordinal))
        {
            selectionMode = PhraseContextAyahSelectionMode.Only;
            return true;
        }

        if (string.Equals(value, "all-except", StringComparison.Ordinal))
        {
            selectionMode = PhraseContextAyahSelectionMode.AllExcept;
            return true;
        }

        return false;
    }
}
