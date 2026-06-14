namespace QuranDashboard.Application.Abstractions.Quran.Tafsirs;

public sealed record TafsirImportResult(
    DateTimeOffset RunAtUtc,
    string Verdict,
    bool Persisted,
    bool Forced,
    TafsirImportTotals Totals,
    IReadOnlyList<TafsirCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);

public sealed record TafsirImportTotals(
    int SourceRows,
    long TafsirTextBlockRows,
    long AyahMappingRows,
    int ApprovedSources,
    int ExcludedSources,
    int ArabicSources,
    int NonArabicSources,
    int LanguageCount,
    int DistinctAyahs)
{
    public static TafsirImportTotals Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);

    public static TafsirImportTotals FromSource(TafsirSourceData source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var arabicSources = source.Sources.Count(row => row.LanguageCode == "ar");
        var languages = source.Sources.Select(row => row.LanguageCode).Distinct().Count();
        var distinctAyahs = source.AyahEntries.Select(row => row.AyahId).Distinct().Count();

        return new TafsirImportTotals(
            source.Sources.Count,
            source.Entries.Count,
            source.AyahEntries.Count,
            source.Sources.Count,
            source.ExcludedSources.Count,
            arabicSources,
            source.Sources.Count - arabicSources,
            languages,
            distinctAyahs);
    }
}

public sealed record TafsirCheckResult(
    string Id,
    string Severity,
    string Expected,
    string Observed,
    bool Passed);
