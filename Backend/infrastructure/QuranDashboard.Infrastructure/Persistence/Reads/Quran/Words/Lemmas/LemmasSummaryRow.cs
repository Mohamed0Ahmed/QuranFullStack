namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;

internal sealed record LemmaSummaryRow(
    int Id,
    string LemmaText,
    string? LemmaBuckwalter,
    string NormalizedLemmaText,
    int? RootId,
    string? RootText,
    string? RootBuckwalter,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    int StemsCount,
    string FirstVerseKey,
    int FirstWordOrderInMushaf,
    IReadOnlyList<LemmaTypeDistributionRow> TypeDistribution);

internal sealed record LemmaTypeDistributionRow(
    string Code,
    string ArabicLabel,
    string EnglishLabel,
    int OccurrencesCount,
    int FirstSurahNumber,
    int FirstAyahNumber,
    int FirstWordNumber);
