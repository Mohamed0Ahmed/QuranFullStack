namespace QuranDashboard.Domain.Quran.Tafsirs;

public sealed class TafsirAyahEntry
{
    public long Id { get; set; }
    public int SourceId { get; set; }
    public int AyahId { get; set; }
    public long TafsirEntryId { get; set; }
    public string VerseKey { get; set; } = string.Empty;
    public string SourceValueKind { get; set; } = string.Empty;
    public string SourceLeaderVerseKey { get; set; } = string.Empty;
    public bool IsGroupLeader { get; set; }
    public int SortOrder { get; set; }
}
