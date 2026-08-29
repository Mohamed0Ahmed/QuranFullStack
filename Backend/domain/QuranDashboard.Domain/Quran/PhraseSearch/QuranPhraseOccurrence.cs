namespace QuranDashboard.Domain.Quran.PhraseSearch;

public sealed class QuranPhraseOccurrence
{
    public Guid BuildId { get; set; }
    public long Id { get; set; }
    public long VariantId { get; set; }
    public PhraseTextMode Mode { get; set; }
    public short WordCount { get; set; }
    public int AyahId { get; set; }
    public short StartWordNumber { get; set; }
    public short EndWordNumber { get; set; }
    public int FirstQuranWordId { get; set; }
    public int LastQuranWordId { get; set; }
}
