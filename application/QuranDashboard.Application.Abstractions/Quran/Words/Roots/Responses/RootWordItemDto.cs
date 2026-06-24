namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

public sealed record RootWordItemDto(
    int UniqueWordId,
    string Kind,
    string DisplayTextUthmani,
    int OccurrencesCount,
    string FirstVerseKey);
