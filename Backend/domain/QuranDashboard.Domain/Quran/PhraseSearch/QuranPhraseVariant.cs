namespace QuranDashboard.Domain.Quran.PhraseSearch;

public sealed class QuranPhraseVariant
{
    public Guid BuildId { get; set; }
    public long Id { get; set; }
    public PhraseTextMode Mode { get; set; }
    public short WordCount { get; set; }
    public int[] ExactTokenIds { get; set; } = [];
    public int[] SearchTokenIds { get; set; } = [];
    public string DisplayText { get; set; } = string.Empty;
    public long OccurrenceCount { get; set; }
    public int AyahCount { get; set; }
    public short SurahCount { get; set; }
    public int FirstQuranWordId { get; set; }
}
