namespace QuranDashboard.Api.Contracts.Linking;

public sealed record LinkingWorkspaceAddSourceBody
{
    public LinkingSourceDescriptorBody? Descriptor { get; init; }

    public uint? WorkspaceVersion { get; init; }
}

public sealed record LinkingWorkspaceReorderBody
{
    public IReadOnlyList<long>? SourceIds { get; init; }

    public uint? WorkspaceVersion { get; init; }
}

public sealed record LinkingWorkspaceConfigurationBody
{
    public uint? SourceVersion { get; init; }

    public string? Label { get; init; }

    public string? InclusionMode { get; init; }

    public IReadOnlyList<int>? AyahOverrides { get; init; }

    public IReadOnlyList<LinkingSelectedWordBody>? SelectedWords { get; init; }

    public bool? AutomaticWordMatchesEnabled { get; init; }

    public string? ManualLinkShape { get; init; }
}

public sealed record LinkingSelectedWordBody
{
    public int? AyahId { get; init; }

    public int? QuranWordId { get; init; }
}
