namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

using System.Text.Json.Serialization;

// One curated secondary-STEM segment decision from segment-stem-corrected-arabic.json.
// `review_decision` is either "approved" (reviewed_stem_text set, bound to a quran_stems row by text)
// or "unresolved_exception" (reviewed_stem_id null, reason set; left null on purpose this feature).
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

// Top-level curated artifact. Only `mappings` is required for the importer; the other metadata fields
// are carried for traceability and ignored during apply.
public sealed record SegmentStemCorrectionArtifact
{
    public const string ApprovedDecision = "approved";
    public const string UnresolvedDecision = "unresolved_exception";

    [JsonPropertyName("feature")] public string? Feature { get; init; }
    [JsonPropertyName("artifactType")] public string? ArtifactType { get; init; }
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("mappings")] public required IReadOnlyList<SegmentStemCorrectionEntry> Mappings { get; init; }
}

// Counts of mappings by decision, computed by the reader.
public sealed record SegmentStemCorrectionCounts(int Total, int Approved, int UnresolvedExceptions);

// Result of Load(): parsed artifact, raw SHA-256, counts, and the two lookups the assembler needs:
// approved secondary segment_location -> reviewed stem TEXT (bound to an id at import), and the set of
// intentional unresolved-exception secondary locations (left null on purpose).
public sealed record SegmentStemCorrectionLoaded(
    SegmentStemCorrectionArtifact Artifact,
    string ArtifactSha256,
    SegmentStemCorrectionCounts Counts,
    IReadOnlyDictionary<string, string> ApprovedStemTextByLocation,
    IReadOnlySet<string> UnresolvedLocations);
