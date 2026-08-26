namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal static class PhraseIndexBuildConstants
{
    internal const int FormatVersion = 1;
    internal const string BuilderVersion = "phrase-index-v1";
    internal const string ApprovedSourceFingerprint =
        "8d9eedffe7b8a490f0e9b597a63229fbe91d3cd4a1bfd32d86d92801167fb075";
    internal const int ExpectedReadableWords = 77_432;
    internal const int ExpectedAyahs = 6_236;
    internal const int ExpectedWindowsPerMode = 795_955;
    internal const int ExpectedWindowsLengthTwoPlusPerMode = 718_523;
    internal const int ExpectedSimpleVariantsLengthTwoPlus = 664_782;
    internal const int ExpectedTashkilVariantsLengthTwoPlus = 669_643;
    internal const int ExpectedTotalVariants = 1_368_351;
    internal const int ExpectedTotalOccurrences = 1_591_910;
    internal const int ExpectedRepeatedVariants = 49_174;
    internal const int ExpectedRepeatedOccurrences = 151_795;
    internal const int ExpectedSimilarityEdges = 1_115_977;
    internal const int MinimumSimilarityLength = 4;
    internal const int CommandTimeoutSeconds = 3_600;

    internal static readonly short[] Thresholds = [50, 60, 70, 80, 90];
}
