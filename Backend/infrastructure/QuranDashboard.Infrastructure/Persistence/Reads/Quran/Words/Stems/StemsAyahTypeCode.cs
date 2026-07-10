namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

internal static class StemsAyahTypeCode
{
    internal const string All = "all";

    internal static string? Normalize(string? typeCode) =>
        string.IsNullOrWhiteSpace(typeCode) ? null : typeCode.Trim();

    internal static string CacheKeyPart(string? typeCode) =>
        Normalize(typeCode) ?? All;
}
