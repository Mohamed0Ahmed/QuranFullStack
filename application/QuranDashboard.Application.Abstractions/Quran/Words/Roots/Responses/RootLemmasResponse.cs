namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

public sealed record RootLemmasResponse(
    int Id,
    string RootText,
    int LemmasCount,
    IReadOnlyList<RootLemmaItemDto> Lemmas);

public sealed record RootLemmaItemDto(
    int LemmaId,
    string LemmaText,
    int OccurrencesCount);
