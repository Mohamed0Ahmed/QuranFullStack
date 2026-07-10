namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;

public sealed record MutashabihatImportResult(
    DateTimeOffset RunAtUtc,
    string Verdict,
    bool Persisted,
    bool Forced,
    MutashabihatImportTotals Totals,
    IReadOnlyList<MutashabihatCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);

public sealed record MutashabihatImportTotals(
    int GroupRows,
    int RawOccurrenceEntries,
    int StoredOccurrenceRows,
    int LinkRows,
    int DistinctSimilarSources,
    int DistinctReferencedAyahs);

public sealed record MutashabihatCheckResult(
    string Id,
    string Severity,
    string Expected,
    string Observed,
    bool Passed);
