namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

// Search carries the raw, trimmed word-identity search term (never logged). It is part of the shared
// Word Types list scope — the words view, all three grouped views, and the scope counts inherit it —
// but grouped-detail reads never set it. Infrastructure normalizes it (ArabicSearchQueryNormalizer)
// for both the SQL predicate and the cache key; an empty/absent value keeps the pre-feature behavior
// and cache key unchanged.
//
// HasRoot/HasStem/HasLemma are tri-state presence flags (Feature 026, US6): null = any, true = must
// have, false = must be missing. Like search they are part of the shared list scope (words + grouped
// views + scope counts reshape together) but never set on grouped-detail reads; absent flags keep the
// pre-feature behavior and cache key unchanged.
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
