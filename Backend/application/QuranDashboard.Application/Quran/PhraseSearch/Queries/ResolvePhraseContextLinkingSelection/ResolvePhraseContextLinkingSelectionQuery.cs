namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.ResolvePhraseContextLinkingSelection;

public sealed record ResolvePhraseContextLinkingSelectionQuery(
    string? Resolution,
    string? Previous,
    string? Following,
    string? PreviousAlternatives,
    string? FollowingAlternatives,
    PhraseContextAyahSelectionMode? SelectionMode,
    IReadOnlyList<int>? AyahIds);

public enum PhraseContextAyahSelectionMode : byte
{
    Only = 1,
    AllExcept = 2,
}
