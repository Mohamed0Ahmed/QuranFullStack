namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

public sealed record UniqueWordMissingSurahsResponse(
    IReadOnlyList<MissingSurahItemDto> Surahs);

public sealed record MissingSurahItemDto(
    int SurahNumber,
    string NameArabic);
