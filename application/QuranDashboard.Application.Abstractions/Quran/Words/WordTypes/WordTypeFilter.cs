namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public sealed record WordTypeFilter(
    string? Type,
    string? ChildCode,
    string? Case,
    string? Tense,
    string? Voice);
