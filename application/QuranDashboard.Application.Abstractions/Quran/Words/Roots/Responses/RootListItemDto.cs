namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

/// <summary>
/// One row of the roots list. Carries the eight aggregate counts. The frontend
/// renders UI row numbers; <see cref="Id"/> is for selection/URL only and is
/// never displayed.
/// </summary>
public sealed record RootListItemDto(
    int Id,
    string RootText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    int LemmasCount,
    int StemsCount,
    string FirstVerseKey);
