namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

public sealed record RootListItemDto(
    int Id,
    string RootText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    int LemmasCount,
    int StemsCount);
