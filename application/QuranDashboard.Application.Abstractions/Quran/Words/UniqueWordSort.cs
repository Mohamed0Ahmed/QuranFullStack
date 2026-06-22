namespace QuranDashboard.Application.Abstractions.Quran.Words;

/// <summary>
/// Unique Words list sort options (Feature 014).
/// <list type="table">
/// <item><c>MushafOrder</c> — default; first occurrence order in the Mushaf.</item>
/// <item><c>Occurrences</c> — highest occurrence count first.</item>
/// <item><c>Alpha</c> — alphabetical order over searchable/display text.</item>
/// </list>
/// </summary>
public enum UniqueWordSort
{
    MushafOrder,
    Occurrences,
    Alpha,
}

/// <summary>Route/JSON key strings for <see cref="UniqueWordSort"/>.</summary>
public static class UniqueWordSortKeys
{
    public const string MushafOrder = "mushaf-order";
    public const string Occurrences = "occurrences";
    public const string Alpha = "alpha";
}

public static class UniqueWordSortParser
{
    /// <summary>
    /// Parses a query <paramref name="value"/> into a <see cref="UniqueWordSort"/>.
    /// Case-insensitive. Returns <see langword="false"/> for unsupported or
    /// empty values so the handler maps to a controlled <c>400</c>.
    /// </summary>
    public static bool TryParse(string? value, out UniqueWordSort sort)
    {
        sort = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case UniqueWordSortKeys.MushafOrder:
                sort = UniqueWordSort.MushafOrder;
                return true;
            case UniqueWordSortKeys.Occurrences:
                sort = UniqueWordSort.Occurrences;
                return true;
            case UniqueWordSortKeys.Alpha:
                sort = UniqueWordSort.Alpha;
                return true;
            default:
                return false;
        }
    }
}
