namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

// Search is the raw user-supplied word term — never logged (privacy).
public sealed record WordTypeFilter(
    string? Type,
    string? ChildCode,
    string? Case,
    string? Tense,
    string? Voice,
    string? Search = null,
    bool? HasRoot = null,
    bool? HasStem = null,
    bool? HasLemma = null);
