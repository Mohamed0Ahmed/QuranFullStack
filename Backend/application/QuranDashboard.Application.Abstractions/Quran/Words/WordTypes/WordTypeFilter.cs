namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public sealed class WordTypeFilter
{
    private const int MaxSearchLength = 64;

    private WordTypeFilter(WordTypeScope scope, string? search, bool? hasRoot, bool? hasStem, bool? hasLemma)
    {
        Scope = scope;
        Search = search;
        HasRoot = hasRoot;
        HasStem = hasStem;
        HasLemma = hasLemma;
    }

    public WordTypeScope Scope { get; }
    public string? Search { get; }
    public bool? HasRoot { get; }
    public bool? HasStem { get; }
    public bool? HasLemma { get; }

    public static WordTypeFilter? Create(
        string? type,
        string? childCode,
        string? @case,
        string? tense,
        string? voice,
        string? search,
        bool? hasRoot,
        bool? hasStem,
        bool? hasLemma)
    {
        var scope = WordTypeScope.Create(type, childCode, @case, tense, voice);
        var normalizedSearch = WordTypeScope.NormalizeOptional(search);
        return scope is null || normalizedSearch?.Length > MaxSearchLength
            ? null
            : new WordTypeFilter(scope, normalizedSearch, hasRoot, hasStem, hasLemma);
    }
}
