namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

// Lives in Application.Abstractions so Application can reference it without depending on Infrastructure.
public sealed record WordLemmaNormalizationSpotCheck(
    string Location,
    string OperationKind,
    string? AppliedLemmaArabic);

public sealed record WordLemmaCorrectionSummary(
    string ArtifactSha256,
    string? RawLemmasSha256,
    int TotalEntries,
    int AppliedAdd,
    int AppliedRemove,
    int AppliedReplace,
    int ReviewedKeep,
    int ReviewedException,
    int FailedOrSkipped,
    IReadOnlyList<WordLemmaNormalizationSpotCheck> SpotChecks)
{
    public int AppliedMutating => AppliedAdd + AppliedRemove + AppliedReplace;
    public int ReviewedNonMutating => ReviewedKeep + ReviewedException;

    public IReadOnlyDictionary<string, int> ProblemClassCounts { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
}
