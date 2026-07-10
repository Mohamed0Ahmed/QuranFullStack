namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

public sealed record UniqueWordListItemDto(
    int Id,
    string Kind,
    string DisplayText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int MissingSurahsCount,
    string? PrimaryWordTypeCode,
    string? PrimaryWordTypeBroadArabicLabel,
    int? RootId,
    string? RootText);
