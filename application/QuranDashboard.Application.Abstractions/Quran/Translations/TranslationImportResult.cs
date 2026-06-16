namespace QuranDashboard.Application.Abstractions.Quran.Translations;

public sealed record TranslationImportResult(
    DateTimeOffset RunAtUtc,
    string Verdict,
    bool Persisted,
    bool Forced,
    TranslationImportTotals Totals,
    IReadOnlyList<TranslationCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);

public sealed record TranslationImportTotals(
    int SourceRows,
    long AyahMappingRows,
    int ApprovedSources,
    int SimpleSources,
    int WithFootnotesSources,
    int ExcludedSources,
    int LanguageCount,
    int DistinctAyahs)
{
    public static TranslationImportTotals Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0);

    public static TranslationImportTotals FromSource(TranslationSourceData source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var simpleSources = source.Sources.Count(row => row.TranslationType == "simple");
        var withFootnotesSources = source.Sources.Count(row => row.TranslationType == "with_footnotes");
        var languages = source.Sources.Select(row => row.LanguageCode).Distinct().Count();
        var distinctAyahs = source.AyahEntries.Select(row => row.AyahId).Distinct().Count();

        return new TranslationImportTotals(
            source.Sources.Count,
            source.AyahEntries.Count,
            source.Sources.Count,
            simpleSources,
            withFootnotesSources,
            source.ExcludedSources.Count,
            languages,
            distinctAyahs);
    }
}

public sealed record TranslationCheckResult(
    string Id,
    string Severity,
    string Expected,
    string Observed,
    bool Passed);
