namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

// Search is the raw, trimmed word-identity term (never logged); infrastructure normalizes it
// (ArabicSearchQueryNormalizer) for both the SQL predicate and the cache key.
// HasRoot/HasStem/HasLemma are tri-state presence flags: null = any, true = must have, false = must be missing.
// Search and the presence flags belong to the shared list scope but are never set on grouped-detail
// reads; absent values keep the pre-feature cache key unchanged.
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
