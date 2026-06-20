namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.SimpleI3rabGeneration;

/// <summary>
/// Matches the divine-name lemma in Buckwalter transliteration.
/// Locked production form from research R7 / coverage report: <c>{ll~ah</c> (not <c>Al~ah</c>).
/// </summary>
public static class AllahLemmaMatcher
{
    internal const string LockedLemmaBuckwalter = "{ll~ah";

    internal static bool IsAllahLemma(string? lemmaBuckwalter) =>
        string.Equals(lemmaBuckwalter, LockedLemmaBuckwalter, StringComparison.Ordinal);
}
