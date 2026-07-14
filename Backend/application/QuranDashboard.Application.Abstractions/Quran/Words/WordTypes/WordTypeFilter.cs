namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

// Search carries the raw, trimmed word-identity search term (never logged). It is part of the shared
// Word Types list scope — the words view, all three grouped views, and the scope counts inherit it —
// but grouped-detail reads never set it. Infrastructure normalizes it (ArabicSearchQueryNormalizer)
// for both the SQL predicate and the cache key; an empty/absent value keeps the pre-feature behavior
// and cache key unchanged.
public sealed record WordTypeFilter(
    string? Type,
    string? ChildCode,
    string? Case,
    string? Tense,
    string? Voice,
    string? Search = null);
