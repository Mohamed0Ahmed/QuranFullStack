namespace QuranDashboard.Domain.Linking;

public sealed class LinkingPreparedUnit
{
    public long Id { get; set; }
    public Guid PreflightId { get; set; }
    public long SourceId { get; set; }
    public int OrderValue { get; set; }
    public string UnitIdentity { get; set; } = string.Empty;
    public byte[] UnitIdentityHash { get; set; } = [];
    public bool IsGrouped { get; set; }
}
