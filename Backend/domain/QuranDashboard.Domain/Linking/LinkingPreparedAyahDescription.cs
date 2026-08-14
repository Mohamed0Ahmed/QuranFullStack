namespace QuranDashboard.Domain.Linking;

public sealed class LinkingPreparedAyahDescription
{
    public long Id { get; set; }
    public long PreparedAyahId { get; set; }
    public int OrderValue { get; set; }
    public string Body { get; set; } = string.Empty;
}
