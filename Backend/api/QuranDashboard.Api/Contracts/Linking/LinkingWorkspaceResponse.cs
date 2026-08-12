namespace QuranDashboard.Api.Contracts.Linking;

public sealed record LinkingWorkspaceResponse
{
    public uint? WorkspaceVersion { get; init; }

    public IReadOnlyList<LinkingWorkspaceSourceResponse> Sources { get; init; } = [];
}

public sealed record LinkingWorkspaceSourceResponse
{
    public long Id { get; init; }

    public uint SourceVersion { get; init; }

    public int OrderValue { get; init; }

    public LinkingSourceDescriptorBody Descriptor { get; init; } = new();

    public string SourceIdentity { get; init; } = string.Empty;

    public string InclusionMode { get; init; } = string.Empty;

    public IReadOnlyList<int> AyahOverrides { get; init; } = [];

    public IReadOnlyList<LinkingSelectedWordBody> SelectedWords { get; init; } = [];

    public bool? AutomaticWordMatchesEnabled { get; init; }

    public string? ManualLinkShape { get; init; }

    public IReadOnlyList<LinkingWorkspaceManualAyahResponse> ManualAyahs { get; init; } = [];

    public IReadOnlyList<LinkingWorkspaceDescriptionResponse> Descriptions { get; init; } = [];

    public int? LastResolvedCount { get; init; }

    public DateTimeOffset? LastResolvedAtUtc { get; init; }
}

public sealed record LinkingWorkspaceManualAyahResponse
{
    public int AyahId { get; init; }

    public string VerseKey { get; init; } = string.Empty;

    public int OrderValue { get; init; }

    public int? PageHint { get; init; }
}

public sealed record LinkingWorkspaceDescriptionResponse
{
    public int AyahId { get; init; }

    public int OrderValue { get; init; }

    public string Body { get; init; } = string.Empty;
}
