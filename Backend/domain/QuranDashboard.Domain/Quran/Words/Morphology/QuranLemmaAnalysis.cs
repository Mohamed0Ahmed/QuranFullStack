namespace QuranDashboard.Domain.Quran.Words.Morphology;

// Analytical breakdown of a display lemma (Feature 020). quran_lemmas holds one row per unique Arabic
// lemma_text; when the Corpus disambiguates homographs with a numeric-suffixed lemma_buckwalter that
// renders to the SAME lemma_text, those distinct source lemmas collapse to one quran_lemmas row but each
// keeps its own analysis row here. This preserves the Corpus root/POS/first-occurrence distinctions
// without duplicating the Arabic display lemma. Occurrence-level truth remains on the segments.
public sealed class QuranLemmaAnalysis
{
    public int Id { get; set; }

    public int LemmaId { get; set; }

    // The distinct Corpus lemma key (internal/audit only, never displayed).
    public string LemmaBuckwalter { get; set; } = string.Empty;

    // The variant's root at its first occurrence. Different-root variants keep different RootId here even
    // when they share the parent lemma's Arabic text (e.g. عَصَا: ع ص و vs ع ص ي).
    public int? RootId { get; set; }

    // The head POS at the variant's first occurrence. A variant may carry different POS across occurrences;
    // only the first-occurrence head POS is stored, and occurrence-level POS stays on the segments.
    public string? HeadPos { get; set; }

    public int WordsCount { get; set; }
    public int FirstWordOrderInMushaf { get; set; }
    public string FirstLocation { get; set; } = string.Empty;

    public QuranLemma? Lemma { get; set; }
    public QuranRoot? Root { get; set; }
}
