namespace QuranDashboard.Domain.Quran.Words.Morphology;

public sealed class QuranRoot
{
    public int Id { get; set; }
    public string RootText { get; set; } = string.Empty;
    public string? RootBuckwalter { get; set; }
    public int WordsCount { get; set; }
    public short DistinctLemmasCount { get; set; }
    public int FirstWordOrderInMushaf { get; set; }
}
