namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

/// <summary>
/// Flat whole-summary row for one stem (Feature 016). Identity, nullable
/// dominant lemma/root relationships, counts, and ordered type distribution are
/// loaded in a single bounded aggregation.
/// </summary>
internal sealed record StemSummaryRow(
    int Id,
    string StemText,
    string NormalizedStemText,
    int? DominantLemmaId,
    string? DominantLemmaText,
    string? DominantLemmaBuckwalter,
    int? DominantRootId,
    string? DominantRootText,
    string? DominantRootBuckwalter,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    string FirstVerseKey,
    int FirstWordOrderInMushaf,
    IReadOnlyList<StemTypeDistributionRow> TypeDistribution);

/// <summary>
/// One POS entry within a stem's type distribution.
/// </summary>
internal sealed record StemTypeDistributionRow(
    string Code,
    string ArabicLabel,
    string EnglishLabel,
    int OccurrencesCount,
    int FirstSurahNumber,
    int FirstAyahNumber,
    int FirstWordNumber);
