using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;
using QuranDashboard.Infrastructure.Files.Quran.Morphology.Irab;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Irab;

public sealed class EfI3rabGenerationSource(QuranDashboardDbContext dbContext, I3rabExpectedCounts expectedCounts)
    : II3rabGenerationSource
{
    public I3rabMorphologyReadiness AssessMorphologyReadiness()
    {
        var segmentCount = dbContext.WordMorphologySegments.AsNoTracking().Count();
        var readableWordCount = dbContext.QuranWords.AsNoTracking().Count(word => !word.IsAyahMarker);
        var nullFormCount = dbContext.WordMorphologySegments.AsNoTracking()
            .Count(segment => segment.FormArabicNormalized == null);
        var morphologyCount = dbContext.WordMorphologies.AsNoTracking().Count();

        if (morphologyCount == 0)
        {
            return I3rabMorphologyReadiness.NotReady(
                segmentCount,
                readableWordCount,
                nullFormCount,
                "Morphology is missing or empty. Run import-morphology first.");
        }

        if (segmentCount != expectedCounts.SegmentCount)
        {
            return I3rabMorphologyReadiness.NotReady(
                segmentCount,
                readableWordCount,
                nullFormCount,
                $"Morphology segment count {segmentCount:N0} does not match the expected {expectedCounts.SegmentCount:N0}.");
        }

        if (readableWordCount != expectedCounts.ReadableWordCount)
        {
            return I3rabMorphologyReadiness.NotReady(
                segmentCount,
                readableWordCount,
                nullFormCount,
                $"Readable word count {readableWordCount:N0} does not match the expected {expectedCounts.ReadableWordCount:N0}.");
        }

        if (nullFormCount != expectedCounts.NullFormCount)
        {
            return I3rabMorphologyReadiness.NotReady(
                segmentCount,
                readableWordCount,
                nullFormCount,
                $"NULL-form segment count {nullFormCount:N0} does not match the expected {expectedCounts.NullFormCount:N0}.");
        }

        return I3rabMorphologyReadiness.Ready(segmentCount, readableWordCount, nullFormCount);
    }

    public bool I3rabAlreadyPopulated() =>
        dbContext.WordMorphologySegments.AsNoTracking().Any(segment =>
            segment.I3rabRuleId != null
            || (segment.I3rabStatus != null
                && segment.I3rabStatus != I3rabStatusMapping.Unsupported));

    public IReadOnlyList<I3rabSegmentInput> LoadSegments()
    {
        return dbContext.WordMorphologySegments
            .AsNoTracking()
            .Join(
                dbContext.WordMorphologies.AsNoTracking(),
                segment => segment.QuranWordId,
                morphology => morphology.QuranWordId,
                (segment, morphology) => new { segment, morphology })
            .OrderBy(row => row.segment.Id)
            .Select(row => new I3rabSegmentInput(
                row.segment.Id,
                row.segment.QuranWordId,
                row.segment.SegmentNumber,
                row.segment.Kind,
                row.segment.Pos,
                row.segment.FeaturesRaw,
                row.morphology.CaseFeature,
                row.morphology.VerbTense,
                row.morphology.VerbVoice,
                row.segment.Kind == "STEM"
                    && row.segment.Pos == "PN"
                    && AllahLemmaMatcher.IsAllahLemma(row.segment.LemmaBuckwalter),
                row.segment.FormArabicNormalized == null))
            .ToList();
    }
}
