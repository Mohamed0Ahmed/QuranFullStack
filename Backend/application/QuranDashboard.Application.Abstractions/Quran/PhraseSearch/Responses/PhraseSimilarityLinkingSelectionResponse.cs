namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

public sealed record PhraseSimilarityLinkingSelectionResponse(
    Guid ActiveBuildId,
    PhraseSimilarityLinkingSelectionQueryDto Query,
    int SelectedAyahCount,
    IReadOnlyList<PhraseSimilarityLinkingSelectionAyahDto> Ayahs);

public sealed record PhraseSimilarityLinkingSelectionQueryDto(
    long VariantId,
    string DisplayText,
    short WordCount);

public sealed record PhraseSimilarityLinkingSelectionAyahDto(
    int AyahId,
    string VerseKey,
    short PageNumber,
    IReadOnlyList<int> SelectedQuranWordIds);
