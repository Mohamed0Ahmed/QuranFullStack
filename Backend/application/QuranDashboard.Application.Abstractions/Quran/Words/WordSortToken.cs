namespace QuranDashboard.Application.Abstractions.Quran.Words;

public static class WordSortToken
{
    public const string AscendingSuffix = "-asc";
    public const string DescendingSuffix = "-desc";

    // Callers MUST allowlist column themselves; this method does not validate it (injection).
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
