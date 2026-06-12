namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public sealed record I3rabGenerationResult(
    bool Persisted,
    bool Forced,
    int SegmentCount,
    int ApprovedCount,
    int NeedsReviewCount,
    int UnsupportedCount,
    int RuleCount,
    int FamilyCount,
    int WordsDisplayable,
    IReadOnlyList<I3rabCheckResult> Checks,
    string? ReportPath);
