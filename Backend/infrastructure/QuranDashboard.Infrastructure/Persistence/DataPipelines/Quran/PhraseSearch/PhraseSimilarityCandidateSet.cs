namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseSimilarityCandidateSet(
    string Algorithm,
    long CandidateEmissions,
    long UniqueCandidates,
    long VerifiedPairs,
    long PeakManagedMemoryBytes,
    bool UsesBruteForce,
    IReadOnlySet<ulong> Candidates);

internal sealed record PhraseVariantVector(long Id, int[] ExactTokenIds);
