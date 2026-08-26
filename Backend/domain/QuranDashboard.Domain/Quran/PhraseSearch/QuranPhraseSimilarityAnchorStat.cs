namespace QuranDashboard.Domain.Quran.PhraseSearch;

public sealed class QuranPhraseSimilarityAnchorStat
{
    public Guid BuildId { get; set; }
    public long VariantId { get; set; }
    public short Threshold { get; set; }
    public PhraseTextMode Mode { get; set; }
    public short WordCount { get; set; }
    public int NeighborCount { get; set; }
    public short? BestMatchedCount { get; set; }
}
