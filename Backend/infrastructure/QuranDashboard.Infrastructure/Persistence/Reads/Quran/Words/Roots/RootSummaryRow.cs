namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

internal sealed record RootSummaryRow(
    int Id,
    string RootText,
    string NormalizedRootText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    int LemmasCount,
    int StemsCount,
    int FirstWordOrderInMushaf,
    IReadOnlyList<RootTypeDistributionRow> TypeDistribution);

internal sealed record RootAggregationRow(
    int Id,
    string RootText,
    string NormalizedRootText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    int LemmasCount,
    int StemsCount,
    int FirstWordOrderInMushaf);

internal sealed record RootTypeDistributionRow(
    int RootId,
    string Code,
    string ArabicLabel,
    int OccurrencesCount,
    int FirstQuranWordId);
