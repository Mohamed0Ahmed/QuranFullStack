namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.SimpleI3rabGeneration;

public static class AllahLemmaMatcher
{
    internal const string LockedLemmaBuckwalter = "{ll~ah";

    internal static bool IsAllahLemma(string? lemmaBuckwalter) =>
        string.Equals(lemmaBuckwalter, LockedLemmaBuckwalter, StringComparison.Ordinal);
}
