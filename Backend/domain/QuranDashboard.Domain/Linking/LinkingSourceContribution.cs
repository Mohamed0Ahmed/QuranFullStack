namespace QuranDashboard.Domain.Linking;

public sealed class LinkingSourceContribution
{
    public long Id { get; set; }

    public long OperationId { get; set; }

    public int DoorId { get; set; }

    public int OrderValue { get; set; }

    public LinkingContributionMode ContributionMode { get; set; }

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

    public int ResolvedAyahCount { get; set; }
    public DateTimeOffset ResolvedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public int CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public int? DeletedBy { get; set; }

    public uint Version { get; set; }
}
