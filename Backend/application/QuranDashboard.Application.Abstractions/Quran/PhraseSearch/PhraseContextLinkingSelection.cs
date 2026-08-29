namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public sealed record PhraseContextLinkingSelection(
    PhraseContextAyahSelectionMode Mode,
    IReadOnlyList<int> AyahIds);

public enum PhraseContextAyahSelectionMode : byte
{
    Only = 1,
    AllExcept = 2,
}
