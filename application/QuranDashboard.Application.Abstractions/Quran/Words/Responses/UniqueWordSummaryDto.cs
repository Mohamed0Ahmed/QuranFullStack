namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

/// <summary>
/// Summary of a selected unique word, used to restore modal state from a
/// shared URL before or alongside a drill-down read. Same shape as a list
/// item without list paging.
/// </summary>
/// <remarks>See Feature 014 data-model.md section D2.</remarks>
public sealed record UniqueWordSummaryDto(
    int Id,
    string Kind,
    string DisplayTextUthmani,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int MissingSurahsCount,
    string FirstVerseKey,
    string FirstLocation);
