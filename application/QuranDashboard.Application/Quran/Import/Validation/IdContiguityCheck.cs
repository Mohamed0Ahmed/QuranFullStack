
using QuranDashboard.Application.Abstractions.Quran.Import;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Application.Quran.Import.Validation;

internal sealed class IdContiguityCheck
{
    public ImportCheckResult Evaluate(IReadOnlyList<QuranWord> words)
    {
        var duplicateIds = words
            .GroupBy(word => word.Id)
            .Count(group => group.Count() > 1);

        var passed = duplicateIds == 0 && IsIdRangeContiguous(words, ImportValidationExpectedCounts.Words);
        var observed = duplicateIds > 0
            ? $"duplicateIds={duplicateIds}"
            : DescribeIdRange(words, ImportValidationExpectedCounts.Words);

        return ImportCheckResults.Hard(
            ImportValidationCheckIds.IdContiguous,
            $"1..{ImportValidationExpectedCounts.Words}",
            observed,
            passed);
    }

    private static bool IsIdRangeContiguous(IReadOnlyList<QuranWord> words, int expectedCount)
    {
        if (words.Count != expectedCount)
        {
            return false;
        }

        var ids = words.Select(word => word.Id).OrderBy(id => id).ToArray();
        for (var index = 0; index < ids.Length; index++)
        {
            if (ids[index] != index + 1)
            {
                return false;
            }
        }

        return true;
    }

    private static string DescribeIdRange(IReadOnlyList<QuranWord> words, int expectedCount)
    {
        if (words.Count == 0)
        {
            return "empty";
        }

        var min = words.Min(word => word.Id);
        var max = words.Max(word => word.Id);
        return words.Count == expectedCount && min == 1 && max == expectedCount
            ? $"1..{expectedCount}"
            : $"{min}..{max} ({words.Count} rows)";
    }
}
