namespace QuranDashboard.Application.Abstractions.Quran.Words;

// A BARE token means the column's natural direction, so every pre-existing token keeps its meaning as
// an alias. Canonicalizing before the cache key is built collapses aliases onto ONE entry per ordering,
// so old links and warm entries stay byte-identical. This type only splits/composes the grammar — each
// explorer's parser owns its column allowlist, so a token never reaches SQL or a LINQ ordering.
public static class WordSortToken
{
    public const string AscendingSuffix = "-asc";
    public const string DescendingSuffix = "-desc";

    // direction is null when the token is bare (use the column's natural direction).
    // Callers MUST still allowlist column — this method does not validate it.
    public static bool TrySplit(string? value, out string column, out WordSortDirection? direction)
    {
        column = string.Empty;
        direction = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var token = value.Trim().ToLowerInvariant();

        if (token.EndsWith(DescendingSuffix, StringComparison.Ordinal))
        {
            direction = WordSortDirection.Descending;
            column = token[..^DescendingSuffix.Length];
        }
        else if (token.EndsWith(AscendingSuffix, StringComparison.Ordinal))
        {
            direction = WordSortDirection.Ascending;
            column = token[..^AscendingSuffix.Length];
        }
        else
        {
            column = token;
        }

        return column.Length > 0;
    }

    public static string Canonical(string columnKey, WordSortDirection direction, WordSortDirection naturalDirection) =>
        direction == naturalDirection
            ? columnKey
            : columnKey + Suffix(direction);

    private static string Suffix(WordSortDirection direction) => direction switch
    {
        WordSortDirection.Ascending => AscendingSuffix,
        WordSortDirection.Descending => DescendingSuffix,
        _ => throw new InvalidOperationException($"Unhandled {nameof(WordSortDirection)} value."),
    };
}
