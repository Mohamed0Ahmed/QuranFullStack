namespace QuranDashboard.Domain.Linking;

public sealed class LinkingWorkspaceSource
{
    public long Id { get; set; }

    public long WorkspaceId { get; set; }

    public int OrderValue { get; set; }

    public LinkingSourceKind SourceKind { get; set; }
    public string SourceIdentity { get; set; } = string.Empty;
    public byte[] SourceIdentityHash { get; set; } = [];
    public string Label { get; set; } = string.Empty;
    public string ScopeJson { get; set; } = string.Empty;

    public int? RootId { get; set; }
    public int? LemmaId { get; set; }
    public int? StemId { get; set; }
    public int? UniqueSimpleWordId { get; set; }
    public int? UniqueTashkeelWordId { get; set; }
    public int? WordTypeTashkeelWordId { get; set; }

    public LinkingInclusionMode InclusionMode { get; set; }
    public bool? AutomaticWordMatchesEnabled { get; set; }
    public LinkingManualLinkShape? ManualLinkShape { get; set; }

    public int? LastResolvedCount { get; set; }
    public DateTimeOffset? LastResolvedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public int CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int UpdatedBy { get; set; }

    public uint Version { get; set; }
}
