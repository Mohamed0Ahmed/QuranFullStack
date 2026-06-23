namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

/// <summary>
/// Single-root summary used to restore panel state from a shared URL. Carries
/// the same eight counts as <see cref="RootListItemDto"/>.
/// </summary>
public sealed record RootSummaryDto(
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
