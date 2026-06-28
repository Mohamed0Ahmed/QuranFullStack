namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

public sealed record RootLemmasResponse(
    IReadOnlyList<RootLemmaItemDto> Lemmas);

public sealed record RootLemmaItemDto(
    int LemmaId,
    string LemmaText,
    int OccurrencesCount);
