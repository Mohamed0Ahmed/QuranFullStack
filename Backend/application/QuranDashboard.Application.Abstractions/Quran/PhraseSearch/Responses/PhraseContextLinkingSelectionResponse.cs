namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

public sealed record PhraseContextLinkingSelectionResponse(
    Guid ActiveBuildId,
    int SelectedAyahCount,
    IReadOnlyList<PhraseContextLinkingSelectionAyahDto> Ayahs);

public sealed record PhraseContextLinkingSelectionAyahDto(
    int AyahId,
    string VerseKey,
    short PageNumber,
    IReadOnlyList<int> SelectedQuranWordIds);
