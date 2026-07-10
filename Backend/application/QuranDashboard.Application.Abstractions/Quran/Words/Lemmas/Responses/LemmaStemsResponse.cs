namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

public sealed record LemmaStemsResponse(
    IReadOnlyList<LemmaStemItemDto> Stems);

public sealed record LemmaStemItemDto(
    int StemId,
    string StemText,
    int OccurrencesCount);
