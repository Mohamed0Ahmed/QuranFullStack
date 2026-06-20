using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Translations;

internal static class TranslationTypeCountValidation
{
    public static TranslationCheckResult BuildTypeCountsCheck(
        TranslationExpectedCounts expected,
        int observedSimple,
        int observedWithFootnotes) =>
        TranslationValidationChecks.Hard(
            TranslationInvariants.CheckTypeCounts,
            $"simple={expected.SimpleSources}, with_footnotes={expected.WithFootnotesSources}",
            $"simple={observedSimple}, with_footnotes={observedWithFootnotes}",
            observedSimple == expected.SimpleSources
                && observedWithFootnotes == expected.WithFootnotesSources);

    public static void EnsureFinalTypeCounts(
        IReadOnlyList<TranslationSourceDto> sources,
        TranslationExpectedCounts expected)
    {
        var simple = sources.Count(source => source.TranslationType == "simple");
        var withFootnotes = sources.Count(source => source.TranslationType == "with_footnotes");
        TranslationValidationChecks.EnsureAllHardChecksPassed([
            BuildTypeCountsCheck(expected, simple, withFootnotes)]);
    }
}
