using System.Globalization;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Application.Abstractions.Linking;

public static class LinkingSourceDescriptorValidation
{
    public const int MinSurahNumber = 1;
    public const int MaxSurahNumber = 114;
    public const int MinAyahNumber = 1;
    public const int MaxAyahNumber = 286;

    private const int MaxVerseKeySegmentDigits = 3;

    public static bool TryValidate(LinkingSourceDescriptor descriptor, out string? error)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        error = string.IsNullOrWhiteSpace(descriptor.Label)
            ? "The source label must be non-blank."
            : DescriptorError(descriptor);

        return error is null;
    }

    private static string? DescriptorError(LinkingSourceDescriptor descriptor) => descriptor switch
    {
        LinkingSourceDescriptor.UniqueWord source => IdentifierError(source.WordId, "wordId"),
        LinkingSourceDescriptor.Root source => IdentifierError(source.RootId, "rootId"),
        LinkingSourceDescriptor.Lemma source =>
            IdentifierError(source.LemmaId, "lemmaId") ?? TypeCodeError(source.TypeCode),
        LinkingSourceDescriptor.Stem source =>
            IdentifierError(source.StemId, "stemId") ?? TypeCodeError(source.TypeCode),
        LinkingSourceDescriptor.WordType source => SelectionError(source.Selection),
        LinkingSourceDescriptor.ManualMushafAyahs source => ManualAyahsError(source.VerseKeys),
        _ => "Unknown source kind.",
    };

    private static string? SelectionError(LinkingWordTypeSelection selection) =>
        ScopeError(selection.Scope) ?? selection switch
        {
            LinkingWordTypeSelection.Word word =>
                IdentifierError(word.TashkeelWordId, "tashkeelWordId")
                ?? (string.IsNullOrWhiteSpace(word.ContextCode) ? "The contextCode must be non-blank." : null)
                ?? TokenError(word.Case, LinkingWordTypeScope.CaseTokens, "case")
                ?? TokenError(word.Tense, LinkingWordTypeScope.TenseTokens, "tense")
                ?? TokenError(word.Voice, LinkingWordTypeScope.VoiceTokens, "voice"),
            LinkingWordTypeSelection.Dimension dimension =>
                IdentifierError(dimension.DimensionId, "selection id"),
            _ => "Unknown word type selection kind.",
        };

    private static string? ScopeError(LinkingWordTypeScope scope) =>
        TokenError(scope.Type, LinkingWordTypeScope.TypeTokens, "scope.type")
        ?? (scope.ChildCode is not null && string.IsNullOrWhiteSpace(scope.ChildCode)
            ? "The scope.childCode must be absent or non-blank."
            : null)
        ?? TokenError(scope.Case, LinkingWordTypeScope.CaseTokens, "scope.case")
        ?? TokenError(scope.Tense, LinkingWordTypeScope.TenseTokens, "scope.tense")
        ?? TokenError(scope.Voice, LinkingWordTypeScope.VoiceTokens, "scope.voice");

    private static string? ManualAyahsError(IReadOnlyList<VerseKey> verseKeys)
    {
        if (verseKeys.Count == 0)
        {
            return "A manual Mushaf source requires at least one verse.";
        }

        foreach (var verseKey in verseKeys)
        {
            if (!IsWellFormedVerseKey(verseKey))
            {
                return $"The verse key '{verseKey.Value}' is not a valid Quran verse reference.";
            }
        }

        return null;
    }

    private static bool IsWellFormedVerseKey(VerseKey verseKey) => VerseKeyError(verseKey.Value) is null;

    private static bool HasTwoDigitSegments(string value)
    {
        var separator = value.IndexOf(':', StringComparison.Ordinal);

        return separator > 0
            && IsDigitSegment(value.AsSpan(0, separator))
            && IsDigitSegment(value.AsSpan(separator + 1));
    }

    private static bool IsDigitSegment(ReadOnlySpan<char> segment) =>
        segment.Length is >= 1 and <= MaxVerseKeySegmentDigits
        && segment.IndexOfAnyExceptInRange('0', '9') < 0;

    public static string? TokenError(string? value, IReadOnlyList<string> vocabulary, string fieldName) =>
        value is not null && vocabulary.Contains(value, StringComparer.Ordinal)
            ? null
            : $"The {fieldName} must be one of: {string.Join(", ", vocabulary)}.";

    public static string? IdentifierError(int value, string fieldName) =>
        value > 0 ? null : $"The {fieldName} must be a positive integer.";

    public static string? RequiredTextError(string? value, string fieldName) =>
        string.IsNullOrWhiteSpace(value) ? $"The {fieldName} must be non-blank." : null;

    public static string? OptionalTextError(string? value, string fieldName) =>
        value is null || !string.IsNullOrWhiteSpace(value)
            ? null
            : $"The {fieldName} must be absent or non-blank.";

    public static string? VerseKeyError(string? verseKey)
    {
        if (verseKey is null || !HasTwoDigitSegments(verseKey))
        {
            return InvalidVerseKeyError(verseKey);
        }

        var separator = verseKey.IndexOf(':', StringComparison.Ordinal);
        var surah = int.Parse(verseKey.AsSpan(0, separator), CultureInfo.InvariantCulture);
        var ayah = int.Parse(verseKey.AsSpan(separator + 1), CultureInfo.InvariantCulture);

        return surah is >= MinSurahNumber and <= MaxSurahNumber
            && ayah is >= MinAyahNumber and <= MaxAyahNumber
                ? null
                : InvalidVerseKeyError(verseKey);
    }

    private static string InvalidVerseKeyError(string? verseKey) =>
        $"The verse key '{verseKey}' is not a valid Quran verse reference.";

    private static string? TypeCodeError(string? typeCode) => OptionalTextError(typeCode, "typeCode");
}
