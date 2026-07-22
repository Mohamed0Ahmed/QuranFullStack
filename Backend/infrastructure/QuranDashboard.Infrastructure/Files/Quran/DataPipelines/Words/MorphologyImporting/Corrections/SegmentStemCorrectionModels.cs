namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;


public sealed record SegmentStemCorrectionEntry
{
    [JsonPropertyName("segment_location")] public required string SegmentLocation { get; init; }
    [JsonPropertyName("segment_id")] public int SegmentId { get; init; }
    [JsonPropertyName("review_decision")] public required string ReviewDecision { get; init; }
    [JsonPropertyName("reviewed_stem_id")] public int? ReviewedStemId { get; init; }
    [JsonPropertyName("reviewed_stem_text")] public string? ReviewedStemText { get; init; }
    [JsonPropertyName("decision_basis")] public string? DecisionBasis { get; init; }
    [JsonPropertyName("reason")] public string? Reason { get; init; }
}

public sealed record SegmentStemCorrectionArtifact
{
    public const string ApprovedDecision = "approved";
    public const string UnresolvedDecision = "unresolved_exception";

    [JsonPropertyName("feature")] public string? Feature { get; init; }
    [JsonPropertyName("artifactType")] public string? ArtifactType { get; init; }
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("mappings")] public required IReadOnlyList<SegmentStemCorrectionEntry> Mappings { get; init; }
}

public sealed record SegmentStemCorrectionCounts(int Total, int Approved, int UnresolvedExceptions);

public sealed record SegmentStemCorrectionLoaded(
    SegmentStemCorrectionArtifact Artifact,
    string ArtifactSha256,
    SegmentStemCorrectionCounts Counts,
    IReadOnlyDictionary<string, string> ApprovedStemTextByLocation,
    IReadOnlySet<string> UnresolvedLocations);
