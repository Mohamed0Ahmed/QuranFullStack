using QuranDashboard.Domain.Quran.MushafPages;

namespace QuranDashboard.Application.Quran.Import.Validation;

internal sealed class PageReconstructionCheck
{
    private static readonly short[] SamplePages = [1, 2, 5, 604];

    public ImportCheckResult Evaluate(
        IReadOnlyList<MushafLine> lines,
        IReadOnlyDictionary<int, QuranWord> wordsById)
    {
        var passed = TryReconstructSamplePages(lines, wordsById, out var observed);

        return ImportCheckResults.Hard(
            ImportValidationCheckIds.PageReconstruct,
            "1,2,5,604 ok",
            observed,
            passed);
    }

    private static bool TryReconstructSamplePages(
        IReadOnlyList<MushafLine> lines,
        IReadOnlyDictionary<int, QuranWord> wordsById,
        out string observed)
    {
        var failedPages = new List<short>();

        foreach (var pageNumber in SamplePages)
        {
            if (!TryReconstructPage(pageNumber, lines, wordsById))
            {
                failedPages.Add(pageNumber);
            }
        }

        observed = failedPages.Count == 0 ? "1,2,5,604 ok" : $"failed pages: {string.Join(',', failedPages)}";
        return failedPages.Count == 0;
    }

    private static bool TryReconstructPage(
        short pageNumber,
        IReadOnlyList<MushafLine> lines,
        IReadOnlyDictionary<int, QuranWord> wordsById)
    {
        var pageLines = lines
            .Where(line => line.PageNumber == pageNumber)
            .OrderBy(line => line.LineNumber)
            .ToList();

        var expectedLineCount = pageNumber is 1 or 2 ? 8 : 15;
        if (pageLines.Count != expectedLineCount)
        {
            return false;
        }

        foreach (var line in pageLines)
        {
            switch (line.LineType)
            {
                case MushafLineType.SurahName:
                case MushafLineType.Basmallah:
                    if (line.FirstWordId is not null || line.LastWordId is not null)
                    {
                        return false;
                    }

                    break;
                case MushafLineType.Ayah:
                    if (line.FirstWordId is null || line.LastWordId is null || line.LastWordId < line.FirstWordId)
                    {
                        return false;
                    }

                    var lineWordOrder = 1;
                    for (var wordId = line.FirstWordId.Value; wordId <= line.LastWordId.Value; wordId++)
                    {
                        if (!wordsById.TryGetValue(wordId, out var word) ||
                            word.PageNumber != pageNumber ||
                            word.LineNumber != line.LineNumber ||
                            word.LineWordOrder != lineWordOrder)
                        {
                            return false;
                        }

                        lineWordOrder++;
                    }

                    break;
                default:
                    return false;
            }
        }

        return true;
    }
}
