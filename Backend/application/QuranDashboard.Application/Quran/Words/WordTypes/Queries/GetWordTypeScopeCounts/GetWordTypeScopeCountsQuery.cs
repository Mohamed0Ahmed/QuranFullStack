namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeScopeCounts;

public sealed record GetWordTypeScopeCountsQuery(
    string? Type,
    string? ChildCode,
    string? Case,
    string? Tense,
    string? Voice,
    string? Search,
    bool? HasRoot = null,
    bool? HasStem = null,
    bool? HasLemma = null);
