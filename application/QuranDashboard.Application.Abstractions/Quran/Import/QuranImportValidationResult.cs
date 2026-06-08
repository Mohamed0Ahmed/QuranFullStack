namespace QuranDashboard.Application.Abstractions.Quran.Import;

public sealed record QuranImportValidationResult(
    DateTimeOffset RunAtUtc,
    string SourceRoot,
    string ManifestVersion,
    string Verdict,
    bool Persisted,
    bool Forced,
    ImportTotals Totals,
    IReadOnlyList<ImportCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);

public sealed record ImportTotals(
    int Surahs,
    int Ayahs,
    int Pages,
    int Lines,
    int Words,
    int AyahMarkers,
    int ReadableWords);

public sealed record ImportCheckResult(
    string Id,
    string Severity,
    string Expected,
    string Observed,
    bool Passed);
