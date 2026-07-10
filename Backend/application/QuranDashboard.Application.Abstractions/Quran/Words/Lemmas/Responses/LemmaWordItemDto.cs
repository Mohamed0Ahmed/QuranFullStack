namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

public sealed record LemmaWordItemDto(
    int UniqueWordId,
    string DisplayText,
    int OccurrencesCount);
