namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

internal sealed record WordTypeGrouping(
    int TashkeelWordId,
    string ContextCode,
    string? Case,
    string? Tense,
    string? Voice);
