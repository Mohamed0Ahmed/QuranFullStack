namespace QuranDashboard.Domain.Quran.FullI3rab;

public sealed class FullI3rabEntry
{
    public long Id { get; set; }
    public int SourceId { get; set; }
    public string SourceEntryKey { get; set; } = string.Empty;
    public int LeaderAyahId { get; set; }
    public string I3rabHtml { get; set; } = string.Empty;
    public short CoveredAyahCount { get; set; }
    public string CoveredAyahKeys { get; set; } = string.Empty;
    public string SourceShape { get; set; } = string.Empty;
    public string TextHash { get; set; } = string.Empty;
}
