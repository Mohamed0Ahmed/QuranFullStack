namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

/// <summary>
/// Internal row mapped from the whole-summary aggregation SQL. EF maps the quoted
/// column aliases to these property names.
/// </summary>
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
