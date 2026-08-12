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
        public UniqueWord(LinkingUniqueWordMode mode, int wordId, string label)
            : base(LinkingSourceKind.UniqueWord, label)
        {
            Mode = mode;
            WordId = LinkingGuard.RequirePositive(wordId, nameof(wordId));
        }

        public LinkingUniqueWordMode Mode { get; }
        public int WordId { get; }
    }

    public sealed class Root : LinkingSourceDescriptor
    {
        public Root(int rootId, string label)
            : base(LinkingSourceKind.Root, label) =>
            RootId = LinkingGuard.RequirePositive(rootId, nameof(rootId));

        public int RootId { get; }
    }

    public sealed class Lemma : LinkingSourceDescriptor
    {
        public Lemma(int lemmaId, string? typeCode, string label)
            : base(LinkingSourceKind.Lemma, label)
        {
            LemmaId = LinkingGuard.RequirePositive(lemmaId, nameof(lemmaId));
            TypeCode = LinkingGuard.RequireAbsentOrNonBlank(typeCode, nameof(typeCode));
        }

        public int LemmaId { get; }
        public string? TypeCode { get; }
    }

    public sealed class Stem : LinkingSourceDescriptor
    {
        public Stem(int stemId, string? typeCode, string label)
            : base(LinkingSourceKind.Stem, label)
        {
            StemId = LinkingGuard.RequirePositive(stemId, nameof(stemId));
            TypeCode = LinkingGuard.RequireAbsentOrNonBlank(typeCode, nameof(typeCode));
        }

        public int StemId { get; }
        public string? TypeCode { get; }
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
        public ManualMushafAyahs(IEnumerable<VerseKey> verseKeys, string label)
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
        }

        public IReadOnlyList<VerseKey> VerseKeys { get; }
    }
}
