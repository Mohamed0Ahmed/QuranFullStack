using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Linking;

internal static class LinkingSourceStorage
{
    private const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions ScopeSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static LinkingSourceStorageForm Encode(
        LinkingSourceDescriptor descriptor,
        string? storedIdentity = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var identity = storedIdentity ?? LinkingSourceIdentity.For(descriptor);
        var form = new LinkingSourceStorageForm
        {
            Kind = descriptor.Kind,
            Label = descriptor.Label,
            SourceIdentity = identity,
            SourceIdentityHash = LinkingSourceIdentity.HashOf(identity),
            ScopeJson = Serialize(ScopeOf(descriptor)),
        };

        return descriptor switch
        {
            LinkingSourceDescriptor.Root source => form with { RootId = source.RootId },
            LinkingSourceDescriptor.Lemma source => form with { LemmaId = source.LemmaId },
            LinkingSourceDescriptor.Stem source => form with { StemId = source.StemId },
            LinkingSourceDescriptor.UniqueWord source => source.Mode == LinkingUniqueWordMode.Simple
                ? form with { UniqueSimpleWordId = source.WordId }
                : form with { UniqueTashkeelWordId = source.WordId },
            LinkingSourceDescriptor.WordType source => WithWordTypeReference(form, source.Selection),
            LinkingSourceDescriptor.ManualMushafAyahs => form,
            _ => throw new ArgumentOutOfRangeException(
                nameof(descriptor), descriptor.Kind, "Unknown linking source kind."),
        };
    }

    public static LinkingSourceDescriptor Decode(
        LinkingWorkspaceSource source,
        IReadOnlyList<string> manualVerseKeys)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(manualVerseKeys);

        var scope = Deserialize(source.ScopeJson);

        return source.SourceKind switch
        {
            LinkingSourceKind.Root => new LinkingSourceDescriptor.Root(
                Require(source.RootId, nameof(source.RootId)), source.Label),
            LinkingSourceKind.Lemma => new LinkingSourceDescriptor.Lemma(
                Require(source.LemmaId, nameof(source.LemmaId)), scope.TypeCode, source.Label),
            LinkingSourceKind.Stem => new LinkingSourceDescriptor.Stem(
                Require(source.StemId, nameof(source.StemId)), scope.TypeCode, source.Label),
            LinkingSourceKind.UniqueWord => source.UniqueSimpleWordId.HasValue
                ? new LinkingSourceDescriptor.UniqueWord(
                    LinkingUniqueWordMode.Simple, source.UniqueSimpleWordId.Value, source.Label)
                : new LinkingSourceDescriptor.UniqueWord(
                    LinkingUniqueWordMode.Tashkeel,
                    Require(source.UniqueTashkeelWordId, nameof(source.UniqueTashkeelWordId)),
                    source.Label),
            LinkingSourceKind.WordType => new LinkingSourceDescriptor.WordType(
                DecodeWordTypeSelection(source, scope), source.Label),
            LinkingSourceKind.ManualMushafAyahs => new LinkingSourceDescriptor.ManualMushafAyahs(
                manualVerseKeys.Select(verseKey => new VerseKey(verseKey)), source.Label),
            _ => throw new ArgumentOutOfRangeException(
                nameof(source), source.SourceKind, "Unknown linking source kind."),
        };
    }

    private static LinkingSourceStorageForm WithWordTypeReference(
        LinkingSourceStorageForm form,
        LinkingWordTypeSelection selection) =>
        selection switch
        {
            LinkingWordTypeSelection.Word word => form with { WordTypeTashkeelWordId = word.TashkeelWordId },
            LinkingWordTypeSelection.Dimension { Kind: LinkingWordTypeSelectionKind.Root } dimension =>
                form with { RootId = dimension.DimensionId },
            LinkingWordTypeSelection.Dimension { Kind: LinkingWordTypeSelectionKind.Stem } dimension =>
                form with { StemId = dimension.DimensionId },
            LinkingWordTypeSelection.Dimension { Kind: LinkingWordTypeSelectionKind.Lemma } dimension =>
                form with { LemmaId = dimension.DimensionId },
            _ => throw new ArgumentOutOfRangeException(
                nameof(selection), selection.Kind, "Unknown word type selection kind."),
        };

    private static LinkingWordTypeSelection DecodeWordTypeSelection(
        LinkingWorkspaceSource source,
        LinkingSourceScopeDocument scope)
    {
        var wordType = scope.WordType
            ?? throw new InvalidOperationException(
                $"Stored word type source {source.Id} carries no word type scope document.");

        var wordTypeScope = new LinkingWordTypeScope(
            wordType.Scope.Type,
            wordType.Scope.ChildCode,
            wordType.Scope.Case,
            wordType.Scope.Tense,
            wordType.Scope.Voice);

        if (!LinkingSourceTokens.TryParseWordTypeSelection(wordType.SelectionKind, out var selectionKind))
        {
            throw new InvalidOperationException(
                $"Stored word type source {source.Id} carries an unknown selection kind '{wordType.SelectionKind}'.");
        }

        if (selectionKind != LinkingWordTypeSelectionKind.Word)
        {
            var dimensionId = selectionKind switch
            {
                LinkingWordTypeSelectionKind.Root => Require(source.RootId, nameof(source.RootId)),
                LinkingWordTypeSelectionKind.Stem => Require(source.StemId, nameof(source.StemId)),
                _ => Require(source.LemmaId, nameof(source.LemmaId)),
            };

            return new LinkingWordTypeSelection.Dimension(selectionKind, dimensionId, wordTypeScope);
        }

        return new LinkingWordTypeSelection.Word(
            Require(source.WordTypeTashkeelWordId, nameof(source.WordTypeTashkeelWordId)),
            wordType.ContextCode
                ?? throw new InvalidOperationException(
                    $"Stored word type source {source.Id} carries no context code."),
            RequireStoredToken(wordType.Case, source.Id, nameof(wordType.Case)),
            RequireStoredToken(wordType.Tense, source.Id, nameof(wordType.Tense)),
            RequireStoredToken(wordType.Voice, source.Id, nameof(wordType.Voice)),
            wordTypeScope);
    }

    private static LinkingSourceScopeDocument ScopeOf(LinkingSourceDescriptor descriptor) =>
        descriptor switch
        {
            LinkingSourceDescriptor.Lemma source => new LinkingSourceScopeDocument { TypeCode = source.TypeCode },
            LinkingSourceDescriptor.Stem source => new LinkingSourceScopeDocument { TypeCode = source.TypeCode },
            LinkingSourceDescriptor.WordType source => new LinkingSourceScopeDocument
            {
                WordType = WordTypeScopeOf(source.Selection),
            },
            _ => new LinkingSourceScopeDocument(),
        };

    private static LinkingWordTypeScopeDocument WordTypeScopeOf(LinkingWordTypeSelection selection)
    {
        var scope = new LinkingWordTypeScopeFields
        {
            Type = selection.Scope.Type,
            ChildCode = selection.Scope.ChildCode,
            Case = selection.Scope.Case,
            Tense = selection.Scope.Tense,
            Voice = selection.Scope.Voice,
        };

        return selection is LinkingWordTypeSelection.Word word
            ? new LinkingWordTypeScopeDocument
            {
                SelectionKind = LinkingSourceTokens.ToToken(selection.Kind),
                ContextCode = word.ContextCode,
                Case = word.Case,
                Tense = word.Tense,
                Voice = word.Voice,
                Scope = scope,
            }
            : new LinkingWordTypeScopeDocument
            {
                SelectionKind = LinkingSourceTokens.ToToken(selection.Kind),
                Scope = scope,
            };
    }

    private static int Require(int? value, string name) =>
        value ?? throw new InvalidOperationException($"Stored linking source is missing '{name}'.");

    private static string RequireStoredToken(string? value, long sourceId, string name) =>
        value ?? throw new InvalidOperationException(
            $"Stored word type source {sourceId} carries no '{name}' token.");

    private static string Serialize(LinkingSourceScopeDocument scope) =>
        JsonSerializer.Serialize(scope, ScopeSerializerOptions);

    private static LinkingSourceScopeDocument Deserialize(string scopeJson) =>
        JsonSerializer.Deserialize<LinkingSourceScopeDocument>(scopeJson, ScopeSerializerOptions)
        ?? throw new InvalidOperationException("Stored linking source scope document is empty.");

    private sealed record LinkingSourceScopeDocument
    {
        public int SchemaVersion { get; init; } = CurrentSchemaVersion;

        public string? TypeCode { get; init; }

        public LinkingWordTypeScopeDocument? WordType { get; init; }
    }

    private sealed record LinkingWordTypeScopeDocument
    {
        public string SelectionKind { get; init; } = string.Empty;

        public string? ContextCode { get; init; }

        public string? Case { get; init; }

        public string? Tense { get; init; }

        public string? Voice { get; init; }

        public LinkingWordTypeScopeFields Scope { get; init; } = new();
    }

    private sealed record LinkingWordTypeScopeFields
    {
        public string Type { get; init; } = string.Empty;

        public string? ChildCode { get; init; }

        public string Case { get; init; } = string.Empty;

        public string Tense { get; init; } = string.Empty;

        public string Voice { get; init; } = string.Empty;
    }
}

internal sealed record LinkingSourceStorageForm
{
    public LinkingSourceKind Kind { get; init; }

    public string Label { get; init; } = string.Empty;

    public string SourceIdentity { get; init; } = string.Empty;

    public byte[] SourceIdentityHash { get; init; } = [];

    public string ScopeJson { get; init; } = string.Empty;

    public int? RootId { get; init; }

    public int? LemmaId { get; init; }

    public int? StemId { get; init; }

    public int? UniqueSimpleWordId { get; init; }

    public int? UniqueTashkeelWordId { get; init; }

    public int? WordTypeTashkeelWordId { get; init; }
}
