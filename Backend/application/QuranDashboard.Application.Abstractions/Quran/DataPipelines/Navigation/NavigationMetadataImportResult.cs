namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;

public sealed record NavigationMetadataImportResult(
    DateTimeOffset RunAtUtc,
    string Verdict,
    bool Persisted,
    bool Forced,
    NavigationImportTotals Totals,
    IReadOnlyList<NavigationCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);

public sealed record NavigationImportTotals(
    int Juz,
    int Hizb,
    int Rub,
    int Sajda,
    int AyahsTagged)
{
    public static NavigationImportTotals Empty { get; } = new(0, 0, 0, 0, 0);
}

public sealed record NavigationCheckResult(
    string Id,
    string Severity,
    string Expected,
    string Observed,
    bool Passed);
