using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Domain.Linking;

internal static class LinkingGuard
{
    public const int MinSurahNumber = 1;
    public const int MaxSurahNumber = 114;
    public const int MinAyahNumber = 1;
    public const int MaxAyahNumber = 286;

    public static VerseKey RequireQuranVerseKey(VerseKey verseKey, string parameterName) =>
        verseKey.Surah is >= MinSurahNumber and <= MaxSurahNumber
        && verseKey.Ayah is >= MinAyahNumber and <= MaxAyahNumber
            ? verseKey
            : throw new ArgumentException(
                $"The verse key '{verseKey.Value}' is not a valid Quran verse reference.",
                parameterName);

    public static string RequireToken(string? token, IReadOnlyList<string> vocabulary, string parameterName) =>
        token is not null && vocabulary.Contains(token, StringComparer.Ordinal)
            ? token
            : throw new ArgumentException(
                $"'{token}' is not one of: {string.Join(", ", vocabulary)}.",
                parameterName);

    public static string? RequireAbsentOrNonBlank(string? value, string parameterName) =>
        value is null || !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("The value must be absent or non-blank.", parameterName);

    public static string RequireNonBlank(string? value, string parameterName) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("The value must be non-blank.", parameterName);

    public static int RequirePositive(int value, string parameterName) =>
        value > 0
            ? value
            : throw new ArgumentOutOfRangeException(parameterName, value, "The identifier must be positive.");
}
