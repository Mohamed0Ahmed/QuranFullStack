namespace QuranDashboard.Application.Abstractions.Quran.FullI3rab;

public sealed record FullI3rabImportReport(
    DateTimeOffset RunAtUtc,
    string Verdict,
    bool Persisted,
    bool Forced,
    string SourcePath,
    FullI3rabImportTotals Totals,
    IReadOnlyList<FullI3rabSourceSummary> SourceSummaries,
    IReadOnlyList<FullI3rabCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);

public sealed record FullI3rabSourceSummary(
    string SourceKey,
    string DisplayNameEn,
    string PackageFile,
    string Sha256,
    string License,
    string Provenance,
    string UsageScope,
    string MarkupFormat,
    bool HasQuranQuotationMarkup);
