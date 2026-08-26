namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseLengthBuildMetric(
    short Mode,
    short WordCount,
    long RawWindows,
    long Variants,
    string Algorithm,
    long CandidateEmissions,
    long UniqueCandidates,
    long VerifiedPairs,
    long Edges,
    long ElapsedMilliseconds,
    long? PeakManagedMemoryBytes);
