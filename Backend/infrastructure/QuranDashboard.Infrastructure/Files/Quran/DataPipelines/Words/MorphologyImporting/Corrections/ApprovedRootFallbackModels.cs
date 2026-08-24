namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

public sealed record ApprovedRootFallbackArtifact(
    int SchemaVersion,
    string Source,
    int ExpectedAppliedCount,
    IReadOnlyList<ApprovedRootFallbackEntry> Entries);

public sealed record ApprovedRootFallbackEntry(
    string Location,
    int QuranWordId,
    short SegmentNumber,
    string ExpectedLemmaBuckwalter,
    string RootBuckwalter,
    string RootArabic,
    string ReviewStatus);

public sealed record ApprovedRootFallbackLoaded(
    ApprovedRootFallbackArtifact Artifact,
    string ArtifactSha256,
    IReadOnlyDictionary<string, ApprovedRootFallbackEntry> EntriesByLocation);
