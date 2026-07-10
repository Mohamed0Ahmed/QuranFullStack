namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

internal static class WordTypeIdentityMatcher
{
    private static readonly HashSet<string> VerbContextCodes = new(StringComparer.Ordinal)
    {
        "past",
        "present",
        "imperative",
        WordTypeRowContext.Unspecified,
    };

    public static bool IsVerbContextCode(string contextCode) => VerbContextCodes.Contains(contextCode);
}

internal static class WordTypeRowContext
{
    public const string Unspecified = "unspecified";
}
