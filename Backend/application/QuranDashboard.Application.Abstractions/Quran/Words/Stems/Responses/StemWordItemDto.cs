namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

public sealed record StemWordItemDto(
    int UniqueWordId,
    string DisplayText,
    int OccurrencesCount);
