namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

// Shared Arabic search-query normalization for the Words explorers. Extracted verbatim from
// EfUniqueWordsReader so the Unique Words and Word Types search boxes fold diacritics and
// orthography identically (research R2). Behavior is unchanged from the original private method:
// diacritics/tatweel are stripped, a fixed hamza/alef/taa-marbuta/alef-maqsura/yaa fold is applied,
// and the result is lower-cased. Whitespace-only or diacritics-only input normalizes to null.
internal static class ArabicSearchQueryNormalizer
{
    private const string FoldFrom = "أإآٱؤئةىي";
    private const string FoldTo = "ااااواهيي";

    public static string? Normalize(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var builder = new StringBuilder(search.Length);
        foreach (var ch in search)
        {
            if (IsSkippable(ch))
            {
                continue;
            }

            builder.Append(Fold(ch));
        }

        var normalized = builder.ToString().ToLowerInvariant();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    public static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static bool IsSkippable(char ch) =>
        ch == 'ـ' ||
        ch is >= 'ؐ' and <= 'ؚ' ||
        ch is >= 'ً' and <= 'ٟ' ||
        ch == 'ٰ' ||
        ch is >= 'ۖ' and <= 'ۭ' ||
        ch is >= '࣓' and <= 'ࣿ';

    private static char Fold(char ch)
    {
        var index = FoldFrom.IndexOf(ch);
        return index >= 0 ? FoldTo[index] : ch;
    }
}
