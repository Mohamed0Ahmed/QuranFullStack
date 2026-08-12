namespace QuranDashboard.Domain.Linking;

public sealed class LinkingWorkspaceSourceDescription
{
    public long Id { get; set; }

    public long WorkspaceSourceId { get; set; }

    public int AyahId { get; set; }

    public int OrderValue { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
    public int CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int UpdatedBy { get; set; }

    public uint Version { get; set; }
}
