namespace QuranDashboard.Api.Contracts.Linking;

public sealed record LinkingWorkspaceAddSourceBody
{
    public LinkingSourceDescriptorBody? Descriptor { get; init; }

    public LinkingWorkspaceInitialConfigurationBody? InitialConfiguration { get; init; }

    public uint? WorkspaceVersion { get; init; }
}

public sealed record LinkingWorkspaceInitialConfigurationBody
{
    public string? InclusionMode { get; init; }

    public IReadOnlyList<int>? AyahOverrides { get; init; }

    public IReadOnlyList<LinkingSelectedWordBody>? SelectedWords { get; init; }

    public bool? AutomaticWordMatchesEnabled { get; init; }

    public string? ManualLinkShape { get; init; }

    public IReadOnlyList<LinkingDescriptionBody>? Descriptions { get; init; }
}

public sealed record LinkingWorkspaceReorderBody
{
    public IReadOnlyList<long>? SourceIds { get; init; }

    public uint? WorkspaceVersion { get; init; }
}

public sealed record LinkingWorkspaceSourceTypesBody
{
    public IReadOnlyList<string>? TypeCodes { get; init; }

    public uint? WorkspaceVersion { get; init; }

    public uint? SourceVersion { get; init; }
}

public sealed record LinkingWorkspaceConfigurationBody
{
    public string? InclusionMode { get; init; }

    public IReadOnlyList<int>? AyahOverrides { get; init; }

    public IReadOnlyList<LinkingSelectedWordBody>? SelectedWords { get; init; }

    public bool? AutomaticWordMatchesEnabled { get; init; }

    public string? ManualLinkShape { get; init; }

    public IReadOnlyList<LinkingDescriptionBody>? Descriptions { get; init; }
}

public sealed record LinkingSelectedWordBody
{
    public int? AyahId { get; init; }

    public int? QuranWordId { get; init; }
}

public sealed record LinkingDescriptionBody
{
    public int? AyahId { get; init; }

    public int? OrderValue { get; init; }

    public string? Body { get; init; }
}
