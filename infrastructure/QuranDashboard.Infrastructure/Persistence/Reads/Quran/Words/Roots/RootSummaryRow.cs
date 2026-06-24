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
    string FirstVerseKey,
    int FirstWordOrderInMushaf);
