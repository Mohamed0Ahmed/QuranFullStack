namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

public sealed record StemLemmasResponse(
    IReadOnlyList<StemLemmaItemDto> Lemmas);

public sealed record StemLemmaItemDto(
    int LemmaId,
    string LemmaText,
    int OccurrencesCount);
