namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public static class PhraseSearchQueryLimits
{
    public const int MaximumDecodedQueryBytes = 4 * 1024;
    public const int MaximumEncodedQueryLength = 5462;
    public const int MaximumResolvedTokens = 128;
    public const int MaximumResolutionCandidates = 25;
}
