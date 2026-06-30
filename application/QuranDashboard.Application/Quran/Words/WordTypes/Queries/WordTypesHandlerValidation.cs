using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries;

internal static class WordTypesHandlerValidation
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;
    public const string DefaultType = "noun";
    public const string DefaultSort = WordTypeSortKeys.Occurrences;

    private static readonly HashSet<string> AllowedTypes = ["noun", "verb", "particle", "inl"];

    public static bool IsValidFilter(WordTypeFilter filter) =>
        filter.Type is null || AllowedTypes.Contains(filter.Type);

    public static string NormalizeType(string? type) =>
        string.IsNullOrWhiteSpace(type) ? DefaultType : type.Trim().ToLowerInvariant();

    public static bool IsValidPaging(int page, int pageSize) =>
        page >= MinPage && pageSize >= MinPageSize && pageSize <= MaxPageSize;
}
