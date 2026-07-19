namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

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

internal sealed record StemTypeDistributionRow(
    string Code,
    string ArabicLabel,
    string EnglishLabel,
    int OccurrencesCount,
    int FirstSurahNumber,
    int FirstAyahNumber,
    int FirstWordNumber);
