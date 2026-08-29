namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public sealed record PhraseSimilarityLinkingSelection(
    PhraseSimilarityAyahSelectionMode Mode,
    IReadOnlyList<int> AyahIds);

public enum PhraseSimilarityAyahSelectionMode : byte
{
    Only = 1,
    AllExcept = 2,
}
