namespace QuranDashboard.Api.Contracts.Linking;

public sealed record LinkingPreflightBody
{
    public int? DoorId { get; init; }

    public IReadOnlyList<LinkingPreflightSourceBody>? Sources { get; init; }
}

public sealed record LinkingPreflightSourceBody
{
    public LinkingSourceDescriptorBody? Descriptor { get; init; }

    public string? ContributionMode { get; init; }

    public bool? AutomaticWordMatchesEnabled { get; init; }

    public int? OrderValue { get; init; }

    public IReadOnlyList<LinkingOperationUnitBody>? Units { get; init; }
}

public sealed record LinkingOperationUnitBody
{
    public IReadOnlyList<LinkingOperationAyahBody>? Ayahs { get; init; }
}

public sealed record LinkingOperationAyahBody
{
    public int? AyahId { get; init; }

    public IReadOnlyList<int>? SelectedWordIds { get; init; }

    public IReadOnlyList<string>? Descriptions { get; init; }
}

public sealed record LinkingConfirmationBody
{
    public int? DoorId { get; init; }

    public string? PreflightToken { get; init; }

    public Guid? IdempotencyKey { get; init; }

    public IReadOnlyList<LinkingConfirmationSourceBody>? Sources { get; init; }
}

public sealed record LinkingConfirmationSourceBody
{
    public LinkingSourceDescriptorBody? Descriptor { get; init; }

    public string? ContributionMode { get; init; }

    public bool? AutomaticWordMatchesEnabled { get; init; }

    public int? OrderValue { get; init; }

    public long? ExistingContributionId { get; init; }

    public uint? ExistingContributionVersion { get; init; }

    public IReadOnlyList<LinkingOperationUnitBody>? Units { get; init; }
}
