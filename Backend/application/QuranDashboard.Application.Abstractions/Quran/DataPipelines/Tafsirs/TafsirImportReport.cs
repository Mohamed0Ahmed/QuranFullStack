namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

public sealed record TafsirImportReport(
    DateTimeOffset RunAtUtc,
    string Verdict,
    bool Persisted,
    bool Forced,
    string Profile,
    string SourcePath,
    TafsirImportTotals Totals,
    IReadOnlyList<TafsirSourceSummary> SourceSummaries,
    IReadOnlyList<TafsirExcludedSourceSummary> ExcludedSourceSummaries,
    IReadOnlyList<TafsirCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);

public sealed record TafsirSourceSummary(
    string SourceKey,
    string LanguageCode,
    string Direction,
    string DisplayNameEn,
    string PackageFile,
    string Sha256,
    string License,
    string Provenance);

public sealed record TafsirExcludedSourceSummary(
    string SourceKey,
    string Status,
    string Reason);
