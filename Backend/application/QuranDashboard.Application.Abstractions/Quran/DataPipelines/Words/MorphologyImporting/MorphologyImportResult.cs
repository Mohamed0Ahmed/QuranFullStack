namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

public sealed record MorphologyImportResult(
    DateTimeOffset RunAtUtc,
    string Verdict,
    bool Persisted,
    bool Forced,
    MorphologyImportTotals Totals,
    IReadOnlyList<MorphologyCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes,
    WordLemmaCorrectionSummary? CorrectionSummary = null,
    ApprovedRootFallbackSummary? RootFallbackSummary = null);

public sealed record MorphologyImportTotals(
    int MorphologyRows,
    int SegmentRows,
    int RootRows,
    int LemmaRows,
    int LemmaAnalysisRows,
    int StemRows,
    int PosTagRows,
    int ReadableWords,
    int EmptyFormRenders,
    IReadOnlyDictionary<string, int> RenderTierCounts);

public sealed record MorphologyCheckResult(
    string Id,
    string Severity,
    string Expected,
    string Observed,
    bool Passed);
