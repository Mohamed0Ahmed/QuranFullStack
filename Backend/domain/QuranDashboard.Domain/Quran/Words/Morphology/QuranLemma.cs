namespace QuranDashboard.Domain.Quran.Words.Morphology;

public sealed class QuranLemma
{
    public int Id { get; set; }
    public string LemmaText { get; set; } = string.Empty;
    public string? LemmaBuckwalter { get; set; }
    public int? RootId { get; set; }
    public int WordsCount { get; set; }
    public int FirstWordOrderInMushaf { get; set; }

    public QuranRoot? Root { get; set; }
}
