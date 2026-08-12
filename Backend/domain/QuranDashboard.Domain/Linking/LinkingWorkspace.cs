namespace QuranDashboard.Domain.Linking;

public sealed class LinkingWorkspace
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public int CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int UpdatedBy { get; set; }

    public uint Version { get; set; }
}
