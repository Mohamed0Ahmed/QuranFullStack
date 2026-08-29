namespace QuranDashboard.Api.Contracts.Linking;

public sealed record LinkingSourceDescriptorBody
{
    public string? Kind { get; init; }

    public string? Label { get; init; }

    public string? Mode { get; init; }

    public int? WordId { get; init; }

    public int? RootId { get; init; }

    public int? LemmaId { get; init; }

    public int? StemId { get; init; }

    public string? TypeCode { get; init; }

    public IReadOnlyList<string>? TypeCodes { get; init; }

    public LinkingWordTypeSelectionBody? Selection { get; init; }

    public IReadOnlyList<LinkingManualAyahBody>? ManualAyahs { get; init; }

    public string? ContextKey { get; init; }
}

public sealed record LinkingWordTypeSelectionBody
{
    public string? Kind { get; init; }

    public int? TashkeelWordId { get; init; }

    public string? ContextCode { get; init; }

    public string? Case { get; init; }

    public string? Tense { get; init; }

    public string? Voice { get; init; }

    public int? RootId { get; init; }

    public int? StemId { get; init; }

    public int? LemmaId { get; init; }

    public LinkingWordTypeScopeBody? Scope { get; init; }
}

public sealed record LinkingWordTypeScopeBody
{
    public string? Type { get; init; }

    public string? ChildCode { get; init; }

    public string? Case { get; init; }

    public string? Tense { get; init; }

    public string? Voice { get; init; }
}

public sealed record LinkingManualAyahBody
{
    public string? VerseKey { get; init; }
}
