using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.ResolvePhraseContextLinkingSelection;

public sealed record ResolvePhraseContextLinkingSelectionQuery(
    string? Resolution,
    string? Previous,
    string? Following,
    string? PreviousAlternatives,
    string? FollowingAlternatives,
    PhraseContextAyahSelectionMode? SelectionMode,
    IReadOnlyList<int>? AyahIds);
