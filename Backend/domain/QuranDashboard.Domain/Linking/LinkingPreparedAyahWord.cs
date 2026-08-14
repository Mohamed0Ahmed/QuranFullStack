namespace QuranDashboard.Domain.Linking;

public sealed class LinkingPreparedAyahWord
{
    public long PreparedAyahId { get; set; }
    public int QuranWordId { get; set; }
    public bool IsSourceMatch { get; set; }
    public bool IsRequested { get; set; }
    public int OrderValue { get; set; }
}
