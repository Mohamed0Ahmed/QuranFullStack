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
    int DistinctAyahs);

public sealed record TafsirCheckResult(
    string Id,
    string Severity,
    string Expected,
    string Observed,
    bool Passed);
