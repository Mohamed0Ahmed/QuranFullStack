namespace QuranDashboard.Domain.Quran.Tafsirs;

public sealed class TafsirEntry
{
    public long Id { get; set; }
    public int SourceId { get; set; }
    public string SourceEntryKey { get; set; } = string.Empty;
    public int LeaderAyahId { get; set; }
    public string TafsirText { get; set; } = string.Empty;
    public short CoveredAyahCount { get; set; }
    public string CoveredAyahKeys { get; set; } = string.Empty;
    public string SourceShape { get; set; } = string.Empty;
    public string TextHash { get; set; } = string.Empty;
}
