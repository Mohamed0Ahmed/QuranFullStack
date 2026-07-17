namespace QuranDashboard.Application.Abstractions.Quran.Words;

/// <summary>
/// The <c>sort</c> token grammar shared by the five Words explorers:
/// <code>token := column | column "-asc" | column "-desc"</code>
/// A BARE token means the column's natural direction (counts descend, text ascends), so every
/// pre-existing token keeps its exact meaning as an alias — <c>occurrences</c> ≡
/// <c>occurrences-desc</c>, <c>alpha</c> ≡ <c>alpha-asc</c>.
/// <para>
/// The bare form is the CANONICAL serialization of a column's natural direction; the suffixed form
/// is canonical only for the opposite direction. Canonicalizing before a cache key is built collapses
/// aliases onto ONE entry per ordering, so old links and warm entries stay byte-identical.
/// </para>
/// <para>
/// This type only decomposes and re-composes the grammar. Each explorer's parser owns its own column
/// allowlist and applies it to the split result — a token never reaches SQL or a LINQ ordering.
/// </para>
/// </summary>
public static class WordSortToken
{
    public const string AscendingSuffix = "-asc";
    public const string DescendingSuffix = "-desc";

    /// <summary>
    /// Splits a raw request token into its column part and its EXPLICIT direction (<c>null</c> when the
    /// token is bare, meaning "use the column's natural direction"). Returns <c>false</c> for
    /// null/whitespace or a token with no column part (e.g. a bare <c>"-asc"</c>).
    /// Callers MUST still allowlist <paramref name="column"/>; this method does not validate it.
    /// </summary>
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

    /// <summary>
    /// Re-composes the canonical token for an already-allowlisted column: the bare
    /// <paramref name="columnKey"/> when <paramref name="direction"/> is the column's
    /// <paramref name="naturalDirection"/>, the suffixed form otherwise.
    /// </summary>
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
