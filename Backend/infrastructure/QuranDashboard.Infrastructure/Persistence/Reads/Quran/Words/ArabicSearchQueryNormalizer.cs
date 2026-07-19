namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

// FoldFrom/FoldTo are the single source of the Arabic fold map: the Roots/Lemmas/Stems/Word-Types
// reader SQL @foldFrom/@foldTo parameters reuse these same constants, so the fold must not be
// re-declared per-derivation (decision 5, DRY).
internal static class ArabicSearchQueryNormalizer
{
    public const string FoldFrom = "أإآٱؤئةىي";
    public const string FoldTo = "ااااواهيي";

    // stripWhitespace=false keeps interior spaces (Unique Words/Word Types default); the
    // Roots/Lemmas/Stems explorers pass true. Whitespace/diacritics-only input normalizes to null.
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
