using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Domain.Linking;

public abstract class LinkingSourceDescriptor
{
    private LinkingSourceDescriptor(LinkingSourceKind kind, string label)
    {
        Kind = kind;
        Label = LinkingGuard.RequireNonBlank(label, nameof(label));
    }

    public LinkingSourceKind Kind { get; }
    public string Label { get; }

    public sealed class UniqueWord : LinkingSourceDescriptor
    {
        public UniqueWord(
            LinkingUniqueWordMode mode,
            int wordId,
            IEnumerable<string>? typeCodes,
            string label)
            : base(LinkingSourceKind.UniqueWord, label)
        {
            Mode = mode;
            WordId = LinkingGuard.RequirePositive(wordId, nameof(wordId));
            TypeCodes = NormalizeTypeCodes(typeCodes);
        }

        public LinkingUniqueWordMode Mode { get; }
        public int WordId { get; }
        public IReadOnlyList<string> TypeCodes { get; }
    }

    public sealed class Root : LinkingSourceDescriptor
    {
        public Root(int rootId, IEnumerable<string>? typeCodes, string label)
            : base(LinkingSourceKind.Root, label)
        {
            RootId = LinkingGuard.RequirePositive(rootId, nameof(rootId));
            TypeCodes = NormalizeTypeCodes(typeCodes);
        }

        public int RootId { get; }
        public IReadOnlyList<string> TypeCodes { get; }
    }

    public sealed class Lemma : LinkingSourceDescriptor
    {
        public Lemma(int lemmaId, IEnumerable<string>? typeCodes, string label)
            : base(LinkingSourceKind.Lemma, label)
        {
            LemmaId = LinkingGuard.RequirePositive(lemmaId, nameof(lemmaId));
            TypeCodes = NormalizeTypeCodes(typeCodes);
        }

        public int LemmaId { get; }
        public IReadOnlyList<string> TypeCodes { get; }
    }

    public sealed class Stem : LinkingSourceDescriptor
    {
        public Stem(int stemId, IEnumerable<string>? typeCodes, string label)
            : base(LinkingSourceKind.Stem, label)
        {
            StemId = LinkingGuard.RequirePositive(stemId, nameof(stemId));
            TypeCodes = NormalizeTypeCodes(typeCodes);
        }

        public int StemId { get; }
        public IReadOnlyList<string> TypeCodes { get; }
    }

    public sealed class WordType : LinkingSourceDescriptor
    {
        public WordType(LinkingWordTypeSelection selection, string label)
            : base(LinkingSourceKind.WordType, label)
        {
            ArgumentNullException.ThrowIfNull(selection);

            Selection = selection;
        }

        public LinkingWordTypeSelection Selection { get; }
    }

    public sealed class ManualMushafAyahs : LinkingSourceDescriptor
    {
        public const int MaxContextKeyLength = 512;

        public ManualMushafAyahs(
            IEnumerable<VerseKey> verseKeys,
            string label,
            string? contextKey = null)
            : base(LinkingSourceKind.ManualMushafAyahs, label)
        {
            ArgumentNullException.ThrowIfNull(verseKeys);

            VerseKeys =
            [
                .. verseKeys
                    .Select(verseKey => LinkingGuard.RequireQuranVerseKey(verseKey, nameof(verseKeys)))
                    .DistinctBy(verseKey => verseKey.Value, StringComparer.Ordinal)
                    .OrderBy(verseKey => verseKey.Surah)
                    .ThenBy(verseKey => verseKey.Ayah)
            ];

            if (VerseKeys.Count == 0)
            {
                throw new ArgumentException(
                    "A manual Mushaf source requires at least one verse.",
                    nameof(verseKeys));
            }

            if (!TryNormalizeContextKey(contextKey, out var normalizedContextKey))
            {
                throw new ArgumentException(
                    $"The context key must be non-blank and no longer than {MaxContextKeyLength} characters.",
                    nameof(contextKey));
            }

            ContextKey = normalizedContextKey;
        }

        public IReadOnlyList<VerseKey> VerseKeys { get; }

        public string? ContextKey { get; }

        public static bool TryNormalizeContextKey(string? contextKey, out string? normalizedContextKey)
        {
            normalizedContextKey = null;

            if (contextKey is null)
            {
                return true;
            }

            var trimmed = contextKey.Trim();
            if (trimmed.Length is 0 or > MaxContextKeyLength)
            {
                return false;
            }

            normalizedContextKey = trimmed;
            return true;
        }
    }

    private static IReadOnlyList<string> NormalizeTypeCodes(IEnumerable<string>? typeCodes) =>
        typeCodes is null
            ? []
            : [
                .. typeCodes
                    .Select(typeCode => LinkingGuard.RequireNonBlank(typeCode, nameof(typeCodes)).Trim())
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
            ];
}
