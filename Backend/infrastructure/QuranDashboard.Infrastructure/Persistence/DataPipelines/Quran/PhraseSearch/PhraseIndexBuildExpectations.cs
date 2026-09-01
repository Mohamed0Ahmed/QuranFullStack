namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

// Production callers receive this single approved baseline. Tests can explicitly replace the scoped
// instance when exercising a declared non-production fixture without changing runtime approval.
internal sealed record PhraseIndexBuildExpectations(
    int ApprovedSourceFingerprintVersion,
    string ApprovedSourceFingerprint,
    int ExpectedWindowsPerMode,
    int ExpectedWindowsLengthTwoPlusPerMode,
    int ExpectedSimpleVariantsLengthTwoPlus,
    int ExpectedTashkilVariantsLengthTwoPlus,
    int ExpectedTotalVariants,
    int ExpectedTotalOccurrences,
    int ExpectedRepeatedVariants,
    int ExpectedRepeatedOccurrences,
    int ExpectedSimilarityEdges,
    IReadOnlyDictionary<short, long> ExpectedThresholdCounts)
{
    internal static PhraseIndexBuildExpectations Production { get; } = new(
        PhraseIndexBuildConstants.ApprovedSourceFingerprintVersion,
        PhraseIndexBuildConstants.ApprovedSourceFingerprint,
        PhraseIndexBuildConstants.ExpectedWindowsPerMode,
        PhraseIndexBuildConstants.ExpectedWindowsLengthTwoPlusPerMode,
        PhraseIndexBuildConstants.ExpectedSimpleVariantsLengthTwoPlus,
        PhraseIndexBuildConstants.ExpectedTashkilVariantsLengthTwoPlus,
        PhraseIndexBuildConstants.ExpectedTotalVariants,
        PhraseIndexBuildConstants.ExpectedTotalOccurrences,
        PhraseIndexBuildConstants.ExpectedRepeatedVariants,
        PhraseIndexBuildConstants.ExpectedRepeatedOccurrences,
        PhraseIndexBuildConstants.ExpectedSimilarityEdges,
        new Dictionary<short, long>
        {
            [50] = 1_115_977,
            [60] = 236_650,
            [70] = 100_789,
            [80] = 33_091,
            [90] = 1_682,
        });
}
