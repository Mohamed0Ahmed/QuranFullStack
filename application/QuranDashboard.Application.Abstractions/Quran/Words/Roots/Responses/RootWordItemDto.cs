namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

public sealed record RootWordItemDto(
    int UniqueWordId,
    string Kind,
    string DisplayText,
    int OccurrencesCount);
