namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

public sealed record LemmaListItemDto(
    int Id,
    string LemmaText,
    int? RootId,
    string? RootText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    int StemsCount);
