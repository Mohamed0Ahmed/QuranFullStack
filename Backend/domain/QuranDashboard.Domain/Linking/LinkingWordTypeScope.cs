namespace QuranDashboard.Domain.Linking;

public sealed class LinkingWordTypeScope
{
    public static readonly IReadOnlyList<string> TypeTokens = ["noun", "verb", "particle", "inl"];

    public static readonly IReadOnlyList<string> CaseTokens =
        ["all", "nominative", "accusative", "genitive", "null"];

    public static readonly IReadOnlyList<string> TenseTokens = ["all", "past", "present", "imperative"];

    public static readonly IReadOnlyList<string> VoiceTokens = ["all", "active", "passive"];

    public LinkingWordTypeScope(string type, string? childCode, string @case, string tense, string voice)
    {
        Type = LinkingGuard.RequireToken(type, TypeTokens, nameof(type));
        ChildCode = LinkingGuard.RequireAbsentOrNonBlank(childCode, nameof(childCode));
        Case = LinkingGuard.RequireToken(@case, CaseTokens, nameof(@case));
        Tense = LinkingGuard.RequireToken(tense, TenseTokens, nameof(tense));
        Voice = LinkingGuard.RequireToken(voice, VoiceTokens, nameof(voice));
    }

    public string Type { get; }
    public string? ChildCode { get; }
    public string Case { get; }
    public string Tense { get; }
    public string Voice { get; }
}

public abstract class LinkingWordTypeSelection
{
    private LinkingWordTypeSelection(LinkingWordTypeSelectionKind kind, LinkingWordTypeScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        Kind = kind;
        Scope = scope;
    }

    public LinkingWordTypeSelectionKind Kind { get; }
    public LinkingWordTypeScope Scope { get; }

    public sealed class Word : LinkingWordTypeSelection
    {
        public Word(
            int tashkeelWordId,
            string contextCode,
            string @case,
            string tense,
            string voice,
            LinkingWordTypeScope scope)
            : base(LinkingWordTypeSelectionKind.Word, scope)
        {
            TashkeelWordId = LinkingGuard.RequirePositive(tashkeelWordId, nameof(tashkeelWordId));
            ContextCode = LinkingGuard.RequireNonBlank(contextCode, nameof(contextCode));
            Case = LinkingGuard.RequireToken(@case, LinkingWordTypeScope.CaseTokens, nameof(@case));
            Tense = LinkingGuard.RequireToken(tense, LinkingWordTypeScope.TenseTokens, nameof(tense));
            Voice = LinkingGuard.RequireToken(voice, LinkingWordTypeScope.VoiceTokens, nameof(voice));
        }

        public int TashkeelWordId { get; }
        public string ContextCode { get; }
        public string Case { get; }
        public string Tense { get; }
        public string Voice { get; }
    }

    public sealed class Dimension : LinkingWordTypeSelection
    {
        public Dimension(LinkingWordTypeSelectionKind kind, int dimensionId, LinkingWordTypeScope scope)
            : base(RequireDimensionKind(kind), scope) =>
            DimensionId = LinkingGuard.RequirePositive(dimensionId, nameof(dimensionId));

        public int DimensionId { get; }

        private static LinkingWordTypeSelectionKind RequireDimensionKind(LinkingWordTypeSelectionKind kind) =>
            kind is LinkingWordTypeSelectionKind.Root
                or LinkingWordTypeSelectionKind.Stem
                or LinkingWordTypeSelectionKind.Lemma
                ? kind
                : throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    kind,
                    "A dimension selection must be root, stem, or lemma.");
    }
}
