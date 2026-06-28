using QuranDashboard.Domain.Quran.Words.Morphology.Irab;

namespace QuranDashboard.Domain.Quran.Words.Morphology;

public sealed class WordMorphologySegment
{
    public int Id { get; set; }
    public int QuranWordId { get; set; }
    public string SegmentLocation { get; set; } = string.Empty;
    public short SegmentNumber { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Pos { get; set; } = string.Empty;
    public string FormBuckwalter { get; set; } = string.Empty;
    public string? FormArabicNormalized { get; set; }
    public string? ArabicRenderTier { get; set; }
    public string ArabicRenderSource { get; set; } = string.Empty;
    public string? RootBuckwalter { get; set; }
    public string? LemmaBuckwalter { get; set; }
    public int? RootId { get; set; }
    public int? LemmaId { get; set; }
    public string FeaturesRaw { get; set; } = string.Empty;
    public string? FeaturesJson { get; set; }
    public string? I3rabArabic { get; set; }
    public int? I3rabRuleId { get; set; }
    public string? I3rabStatus { get; set; }
    public string? I3rabReviewReason { get; set; }

    public Words.QuranWord QuranWord { get; set; } = null!;
    public QuranRoot? Root { get; set; }
    public QuranLemma? Lemma { get; set; }
    public QuranI3rabRule? I3rabRule { get; set; }
}
