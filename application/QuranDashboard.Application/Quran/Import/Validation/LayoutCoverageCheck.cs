using QuranDashboard.Domain.Quran.MushafPages;

namespace QuranDashboard.Application.Quran.Import.Validation;

internal sealed class LayoutCoverageCheck
{
    public ImportCheckResult Evaluate(IReadOnlyList<MushafLine> lines)
    {
        var passed = TryDescribeLayoutCoverage(lines, out var observed);

        return ImportCheckResults.Hard(
            ImportValidationCheckIds.LayoutCoverage,
            $"1..{ImportValidationExpectedCounts.Words} no gaps",
            observed,
            passed);
    }

    private static bool TryDescribeLayoutCoverage(IReadOnlyList<MushafLine> lines, out string observed)
    {
        var covered = new HashSet<int>();
        var overlapCount = 0;

        foreach (var line in lines.Where(line => line.LineType == MushafLineType.Ayah))
        {
            if (line.FirstWordId is null || line.LastWordId is null || line.LastWordId < line.FirstWordId)
            {
                observed = "invalid ayah line range";
                return false;
            }

            for (var wordId = line.FirstWordId.Value; wordId <= line.LastWordId.Value; wordId++)
            {
                if (!covered.Add(wordId))
                {
                    overlapCount++;
                }
            }
        }

        if (overlapCount > 0)
        {
            observed = $"overlaps={overlapCount}";
            return false;
        }

        if (covered.Count != ImportValidationExpectedCounts.Words)
        {
            observed = $"covered {covered.Count}";
            return false;
        }

        for (var wordId = 1; wordId <= ImportValidationExpectedCounts.Words; wordId++)
        {
            if (!covered.Contains(wordId))
            {
                observed = $"gap at {wordId}";
                return false;
            }
        }

        observed = $"1..{ImportValidationExpectedCounts.Words} no gaps";
        return true;
    }
}
