namespace QuranDashboard.Domain.Quran.Words.Morphology;

public sealed class QuranLemmaAnalysis
{
    public int Id { get; set; }

    public int LemmaId { get; set; }

    public string LemmaBuckwalter { get; set; } = string.Empty;

    public int? RootId { get; set; }

    public string? HeadPos { get; set; }

    public int WordsCount { get; set; }
    public int FirstWordOrderInMushaf { get; set; }
    public string FirstLocation { get; set; } = string.Empty;

    public QuranLemma? Lemma { get; set; }
    public QuranRoot? Root { get; set; }
}
