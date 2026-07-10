namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;

public sealed record NavigationMetadataImportReport(
    string Feature,
    DateTimeOffset RunAtUtc,
    string Verdict,
    bool Persisted,
    bool Forced,
    string SourcePath,
    NavigationManifestSummary Manifest,
    NavigationImportTotals Totals,
    NavigationAyahCoverageSummary AyahCoverage,
    IReadOnlyList<NavigationCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    bool NoQuranAyahTextReadOrStored);

public sealed record NavigationManifestSummary(
    string PackageType,
    bool IsFinalImportManifest);

public sealed record NavigationAyahCoverageSummary(
    int TotalAyahs,
    int WithJuz,
    int WithHizb,
    int WithRub,
    bool Complete);
