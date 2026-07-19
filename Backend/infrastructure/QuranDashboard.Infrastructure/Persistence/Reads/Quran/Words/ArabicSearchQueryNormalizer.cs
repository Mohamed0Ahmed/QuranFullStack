namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

// Shared Arabic search-query normalization for the Words explorers. Extracted verbatim from
// EfUniqueWordsReader so the Unique Words and Word Types search boxes fold diacritics and
// orthography identically (research R2). Behavior is unchanged from the original private method:
// diacritics/tatweel are stripped, a fixed hamza/alef/taa-marbuta/alef-maqsura/yaa fold is applied,
// and the result is lower-cased. Whitespace-only or diacritics-only input normalizes to null.
//
// decision 5 (DRY): FoldFrom/FoldTo are also the single source for the Roots/Lemmas/Stems/Word-Types
// reader SQL @foldFrom/@foldTo parameter values (see EfRootsReader, EfLemmasReader,
// EfStemsReader.Summary, EfWordTypesReader.GroupedTable.Sql) — do not re-declare this fold map
// per-derivation.
internal static class ArabicSearchQueryNormalizer
{
    public const string FoldFrom = "أإآٱؤئةىي";
    public const string FoldTo = "ااااواهيي";

    /// <summary>
    /// Normalizes an Arabic search query: strips diacritics/tatweel, folds hamza/alef/taa-marbuta/
    /// alef-maqsura/yaa variants, and lower-cases. Interior spaces are kept by default (matching the
    /// original Unique Words/Word Types behavior). Pass <paramref name="stripWhitespace"/> true to also
    /// remove whitespace (the Roots/Lemmas/Stems explorers' behavior, decision 5 (DRY) consolidation of
    /// their formerly-duplicated <c>NormalizeArabicQuery</c> copies). Whitespace/diacritics-only input
    /// normalizes to null either way.
    /// </summary>
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
