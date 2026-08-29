namespace QuranDashboard.Domain.Quran.PhraseSearch;

public sealed class PhraseIndexBuild
{
    public Guid Id { get; set; }
    public PhraseIndexBuildStatus Status { get; set; }
    public int FormatVersion { get; set; }
    public bool ExactReady { get; set; }
    public bool SimilarityReady { get; set; }
    public string BuilderVersion { get; set; } = string.Empty;
    public long SourceRevision { get; set; }
    public string SourceFingerprint { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? ValidatedAtUtc { get; set; }
    public DateTimeOffset? ActivatedAtUtc { get; set; }
    public DateTimeOffset? FailedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public long SearchTokenCount { get; set; }
    public long VariantCount { get; set; }
    public long OccurrenceCount { get; set; }
    public long SimilarityEdgeCount { get; set; }
    public long SimilarityAnchorStatCount { get; set; }
    public string? ValidationVerdict { get; set; }
    public string? ReportPath { get; set; }
    public string? FailureSummary { get; set; }
}
