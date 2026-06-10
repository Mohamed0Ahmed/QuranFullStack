namespace QuranDashboard.Domain.Quran.Words.Morphology;

public sealed class QuranStem
{
    public int Id { get; set; }
    public string StemText { get; set; } = string.Empty;
    public int WordsCount { get; set; }
    public int FirstWordOrderInMushaf { get; set; }
}
