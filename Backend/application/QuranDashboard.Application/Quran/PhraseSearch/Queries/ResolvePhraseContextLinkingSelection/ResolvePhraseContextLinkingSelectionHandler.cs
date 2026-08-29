using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.ResolvePhraseContextLinkingSelection;

public sealed class ResolvePhraseContextLinkingSelectionHandler(PhraseContextRequestParser parser)
{
    internal bool TryParse(
        ResolvePhraseContextLinkingSelectionQuery query,
        out PhraseContextLinkingSelectionRequest? request)
    {
        ArgumentNullException.ThrowIfNull(query);
        request = null;
        if (query.SelectionMode is null
            || !Enum.IsDefined(query.SelectionMode.Value)
            || query.AyahIds is null
            || !parser.TryParseSelection(
                query.Resolution,
                query.Previous,
                query.Following,
                query.PreviousAlternatives,
                query.FollowingAlternatives,
                out var selection)
            || selection is null)
        {
            return false;
        }

        request = new PhraseContextLinkingSelectionRequest(
            selection,
            query.SelectionMode.Value,
            query.AyahIds);
        return true;
    }
}

public sealed record PhraseContextLinkingSelectionRequest(
    PhraseContextSelection Selection,
    PhraseContextAyahSelectionMode SelectionMode,
    IReadOnlyList<int> AyahIds);
