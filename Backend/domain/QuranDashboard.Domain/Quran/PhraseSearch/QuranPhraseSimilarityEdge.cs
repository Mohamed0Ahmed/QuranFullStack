namespace QuranDashboard.Domain.Quran.PhraseSearch;

public sealed class QuranPhraseSimilarityEdge
{
    public Guid BuildId { get; set; }
    public PhraseTextMode Mode { get; set; }
    public short WordCount { get; set; }
    public long LeftVariantId { get; set; }
    public long RightVariantId { get; set; }
    public short MatchedCount { get; set; }
    public short DifferenceCount { get; set; }
    public short[] DifferencePositions { get; set; } = [];
}
