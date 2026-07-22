namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

internal static class ArabicSearchQueryNormalizer
{
    public const string FoldFrom = "أإآٱؤئةىي";
    public const string FoldTo = "ااااواهيي";

    public static string? Normalize(string? search, bool stripWhitespace = false)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var builder = new StringBuilder(search.Length);
        foreach (var ch in search)
        {
            if (IsSkippable(ch) || (stripWhitespace && char.IsWhiteSpace(ch)))
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
