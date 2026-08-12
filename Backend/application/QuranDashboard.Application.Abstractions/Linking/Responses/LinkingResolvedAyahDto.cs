namespace QuranDashboard.Application.Abstractions.Linking.Responses;

public sealed record LinkingResolvedAyahDto(
    int AyahId,
    string VerseKey,
    int SurahNumber,
    int AyahNumber,
    string SurahNameArabic,
    short PageFrom,
    short PageTo,
    IReadOnlyList<int> MatchedQuranWordIds,
    IReadOnlyList<LinkingResolvedWordDto> Words);
