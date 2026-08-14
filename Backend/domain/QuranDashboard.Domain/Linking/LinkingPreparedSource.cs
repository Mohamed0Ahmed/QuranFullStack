namespace QuranDashboard.Domain.Linking;

public sealed class LinkingPreparedSource
{
    public long Id { get; set; }
    public Guid PreflightId { get; set; }
    public int OrderValue { get; set; }
    public string ResolutionIdentity { get; set; } = string.Empty;
    public byte[] ResolutionIdentityHash { get; set; } = [];
    public string ContributionIdentity { get; set; } = string.Empty;
    public byte[] ContributionIdentityHash { get; set; } = [];
    public string Label { get; set; } = string.Empty;
    public LinkingSourceKind SourceKind { get; set; }
    public LinkingContributionMode ContributionMode { get; set; }
    public int DescriptorSchemaVersion { get; set; }
    public string DescriptorDocumentJson { get; set; } = string.Empty;
    public int ConfigurationSchemaVersion { get; set; }
    public string ConfigurationDocumentJson { get; set; } = string.Empty;
    public long? WorkspaceSourceId { get; set; }
    public uint? SourceVersion { get; set; }
    public bool? AutomaticWordMatchesEnabled { get; set; }
    public long? ExistingContributionId { get; set; }
    public uint? ExpectedContributionVersion { get; set; }
    public string? Classification { get; set; }
    public int? RequestedCount { get; set; }
    public int? NewCount { get; set; }
    public int? OverlappingCount { get; set; }
    public int? UnchangedCount { get; set; }
    public int? UpdatedCount { get; set; }
    public int? RemovedCount { get; set; }
    public int? InvalidCount { get; set; }
    public int? TotalAyahCount { get; set; }
}
