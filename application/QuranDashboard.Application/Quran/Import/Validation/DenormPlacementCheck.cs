using QuranDashboard.Domain.Quran.MushafPages;

namespace QuranDashboard.Application.Quran.Import.Validation;

internal sealed class DenormPlacementCheck
{
    public ImportCheckResult Evaluate(
        IReadOnlyList<MushafLine> lines,
        IReadOnlyDictionary<int, QuranWord> wordsById)
    {
        var mismatches = CountDenormMismatches(lines, wordsById);

        return ImportCheckResults.Hard(
            ImportValidationCheckIds.DenormPageLine,
            "match",
            mismatches == 0 ? "match" : $"{mismatches} mismatches",
            mismatches == 0);
    }

    private static int CountDenormMismatches(
        IReadOnlyList<MushafLine> lines,
        IReadOnlyDictionary<int, QuranWord> wordsById)
    {
        var mismatches = 0;

        foreach (var line in lines.Where(line => line.LineType == MushafLineType.Ayah))
        {
            if (line.FirstWordId is null || line.LastWordId is null)
            {
                mismatches++;
                continue;
            }

            var lineWordOrder = 1;
            for (var wordId = line.FirstWordId.Value; wordId <= line.LastWordId.Value; wordId++)
            {
                if (!wordsById.TryGetValue(wordId, out var word) ||
                    word.PageNumber != line.PageNumber ||
                    word.LineNumber != line.LineNumber ||
                    word.LineWordOrder != lineWordOrder)
                {
                    mismatches++;
                }

                lineWordOrder++;
            }
        }

        return mismatches;
    }
}
