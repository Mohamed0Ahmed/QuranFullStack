namespace QuranDashboard.Domain.Linking;

public sealed class LinkingPreparedAyah
{
    public long Id { get; set; }
    public Guid PreflightId { get; set; }
    public long SourceId { get; set; }
    public long? UnitId { get; set; }
    public bool IsRequested { get; set; }
    public int SourceOrder { get; set; }
    public int UnitOrder { get; set; }
    public int AyahOrder { get; set; }
    public int QuranOrder { get; set; }
    public bool IsGrouped { get; set; }
    public int AyahId { get; set; }
    public string Classification { get; set; } = string.Empty;
    public string? InvalidReason { get; set; }
    public string ClassificationImpactJson { get; set; } = string.Empty;
}
