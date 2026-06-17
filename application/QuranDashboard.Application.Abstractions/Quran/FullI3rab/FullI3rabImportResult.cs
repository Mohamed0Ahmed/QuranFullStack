namespace QuranDashboard.Application.Abstractions.Quran.FullI3rab;

public sealed record FullI3rabImportResult(
    DateTimeOffset RunAtUtc,
    string Verdict,
    bool Persisted,
    bool Forced,
    FullI3rabImportTotals Totals,
    IReadOnlyList<FullI3rabCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);

public sealed record FullI3rabImportTotals(
    int SourceRows,
    long EntryRows,
    long AyahMappingRows,
    int DistinctAyahs)
{
    public static FullI3rabImportTotals Empty { get; } = new(0, 0, 0, 0);

    public static FullI3rabImportTotals FromSource(FullI3rabSourceData source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var distinctAyahs = source.AyahEntries.Select(row => row.AyahId).Distinct().Count();

        return new FullI3rabImportTotals(
            source.Sources.Count,
            source.Entries.Count,
            source.AyahEntries.Count,
            distinctAyahs);
    }
}

public sealed record FullI3rabCheckResult(
    string Id,
    string Severity,
    string Expected,
    string Observed,
    bool Passed);
