namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public sealed record WordTypeRowIdentity(
    int TashkeelWordId,
    string ContextCode,
    string? Case,
    string? Tense,
    string? Voice)
{
    public bool IsValid => TashkeelWordId > 0 && !string.IsNullOrWhiteSpace(ContextCode);
}
