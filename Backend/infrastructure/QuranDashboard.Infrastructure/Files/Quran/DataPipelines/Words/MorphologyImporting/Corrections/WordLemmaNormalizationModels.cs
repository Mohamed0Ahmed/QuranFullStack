namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

using System.Text.Json.Serialization;

// Operation kinds supported by the word-level lemma normalization artifact.
// Order mirrors the artifact spec (see word-level-lemma-full-normalization-implementation-plan.md §3.2).
[JsonConverter(typeof(WordLemmaOperationKindJsonConverter))]
public enum WordLemmaNormalizationOperationKind
{
    Add,
    Remove,
    Replace,
    Keep,
    Exception,
}

// Final decision statuses permitted in the active (embedded) artifact.
// `Candidate` and `NeedsReview` are draft-only: they parse so the validator can fail closed with a
// precise message, but they must never appear in the active artifact.
[JsonConverter(typeof(WordLemmaDecisionStatusJsonConverter))]
public enum WordLemmaNormalizationDecisionStatus
{
    Approved,
    AcceptedException,
    Candidate,
    NeedsReview,
}

// One curated correction in the normalization artifact.
// `expectedCurrentLemmaArabic`/`correctedLemmaArabic` nullability rules are enforced by the validator
// per operation kind.
public sealed record WordLemmaNormalizationEntry
{
    public required string Id { get; init; }
    public required string Location { get; init; }
    public required WordLemmaNormalizationOperationKind OperationKind { get; init; }
    public string? ExpectedCurrentLemmaArabic { get; init; }
    public string? CorrectedLemmaArabic { get; init; }
    public string? WordTextUthmani { get; init; }
    public string? CorpusLemmaBuckwalter { get; init; }
    public string? CorpusRootBuckwalter { get; init; }
    public string? CorpusPos { get; init; }
    public IReadOnlyList<string> CurrentLocationCorpusLemmaBuckwalters { get; init; } = [];
    public string? ArabicMappingEvidence { get; init; }
    public required WordLemmaNormalizationDecisionStatus DecisionStatus { get; init; }
    public string? Confidence { get; init; }
    public string? ProblemClass { get; init; }
    public string? RelatedLocation { get; init; }
    public bool IsMultiStem { get; init; }
    public string? Reason { get; init; }
    public string? SourceReportRef { get; init; }
}

// Top-level active normalization artifact (schema version 2).
public sealed record WordLemmaNormalizationArtifact
{
    public const int SupportedSchemaVersion = 2;

    public required int SchemaVersion { get; init; }
    public required string ArtifactId { get; init; }
    public string? SourcePackage { get; init; }
    public string? SourceAudit { get; init; }
    public bool ImplementationReady { get; init; }
    public string? ImplementationReadyReason { get; init; }
    public IReadOnlyList<string> GeneratedFromReports { get; init; } = [];
    public string? Note { get; init; }
    public required IReadOnlyList<WordLemmaNormalizationEntry> Entries { get; init; }
}

// One Buckwalter→Arabic lemma mapping evidence row from word-lemma-mapping-evidence.json.
// Provenance fields stay for traceability; validator only requires a matching curated row for the
// (buckwalter, arabicLemma) pair and does not enforce the evidence metadata fields.
public sealed record WordLemmaMappingEvidenceEntry
{
    public required string Buckwalter { get; init; }
    public required string ArabicLemma { get; init; }
    public int SupportingCount { get; init; }
    public int TotalCount { get; init; }
    public double DominancePct { get; init; }
    public required string AmbiguityStatus { get; init; }
    public IReadOnlyList<string> Examples { get; init; } = [];
    public bool AllowAutoAddReplace { get; init; }
    public string? Basis { get; init; }
}

// Counts of entries by operation kind and decision status, computed by the reader.
public sealed record WordLemmaNormalizationCounts(
    int Total,
    int Add,
    int Remove,
    int Replace,
    int Keep,
    int Exception,
    int Approved,
    int AcceptedException,
    int CandidateOrNeedsReview);

// Output of applying the artifact to an in-memory raw QUL lemma map.
public sealed record WordLemmaNormalizationResult(
    IReadOnlyDictionary<string, string> CorrectedLemmas,
    WordLemmaCorrectionSummary Summary);

// Reads JSON operationKind values verbatim (lowercase: add/remove/replace/keep/exception) and maps
// them to the enum by name. Anything else throws -> fail-closed on an unknown kind.
file sealed class WordLemmaOperationKindJsonConverter : JsonConverter<WordLemmaNormalizationOperationKind>
{
    public override WordLemmaNormalizationOperationKind Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (reader.ValueTextEquals("add")) return WordLemmaNormalizationOperationKind.Add;
            if (reader.ValueTextEquals("remove")) return WordLemmaNormalizationOperationKind.Remove;
            if (reader.ValueTextEquals("replace")) return WordLemmaNormalizationOperationKind.Replace;
            if (reader.ValueTextEquals("keep")) return WordLemmaNormalizationOperationKind.Keep;
            if (reader.ValueTextEquals("exception")) return WordLemmaNormalizationOperationKind.Exception;
        }

        throw new JsonException(
            $"Unknown word-lemma normalization operationKind: {reader.GetString() ?? "<null>"}.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        WordLemmaNormalizationOperationKind value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
}

// Reads JSON decisionStatus values: `approved`, `accepted-exception`, and the draft-only
// `candidate` / `needs-review` (parsed so the validator can fail closed with a precise message).
file sealed class WordLemmaDecisionStatusJsonConverter : JsonConverter<WordLemmaNormalizationDecisionStatus>
{
    public override WordLemmaNormalizationDecisionStatus Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (reader.ValueTextEquals("approved")) return WordLemmaNormalizationDecisionStatus.Approved;
            if (reader.ValueTextEquals("accepted-exception")) return WordLemmaNormalizationDecisionStatus.AcceptedException;
            if (reader.ValueTextEquals("candidate")) return WordLemmaNormalizationDecisionStatus.Candidate;
            if (reader.ValueTextEquals("needs-review")) return WordLemmaNormalizationDecisionStatus.NeedsReview;
        }

        throw new JsonException(
            $"Unknown word-lemma normalization decisionStatus: {reader.GetString() ?? "<null>"}.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        WordLemmaNormalizationDecisionStatus value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            WordLemmaNormalizationDecisionStatus.AcceptedException => "accepted-exception",
            WordLemmaNormalizationDecisionStatus.NeedsReview => "needs-review",
            _ => value.ToString().ToLowerInvariant(),
        });
}
