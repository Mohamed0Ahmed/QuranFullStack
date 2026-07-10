namespace QuranDashboard.Domain.Quran.Words.Morphology;

public sealed class WordMorphology
{
    public int QuranWordId { get; set; }
    public string Location { get; set; } = string.Empty;
    public string HeadPos { get; set; } = string.Empty;
    public short SegmentCount { get; set; }
    public int? RootId { get; set; }
    public int? LemmaId { get; set; }
    public int? StemId { get; set; }
    public bool IsVerb { get; set; }
    public string? VerbTense { get; set; }
    public string? VerbVoice { get; set; }
    public string? CaseFeature { get; set; }
    public string? HeadFeaturesJson { get; set; }

    public Words.QuranWord QuranWord { get; set; } = null!;
    public QuranRoot? Root { get; set; }
    public QuranLemma? Lemma { get; set; }
    public QuranStem? Stem { get; set; }
}
