namespace QuranDashboard.Domain.Quran.FullI3rab;

public sealed class FullI3rabAyahEntry
{
    public long Id { get; set; }
    public int SourceId { get; set; }
    public int AyahId { get; set; }
    public long EntryId { get; set; }
    public string VerseKey { get; set; } = string.Empty;
    public string SourceValueKind { get; set; } = string.Empty;
    public string SourceLeaderVerseKey { get; set; } = string.Empty;
    public bool IsGroupLeader { get; set; }
    public int SortOrder { get; set; }
}
