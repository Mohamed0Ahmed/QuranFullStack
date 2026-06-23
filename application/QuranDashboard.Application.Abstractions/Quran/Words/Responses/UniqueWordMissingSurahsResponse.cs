namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

public sealed record UniqueWordMissingSurahsResponse(
    int Id,
    string Kind,
    string DisplayTextUthmani,
    int MissingSurahsCount,
    IReadOnlyList<MissingSurahItemDto> Surahs);

public sealed record MissingSurahItemDto(
    int SurahNumber,
    string NameArabic);
