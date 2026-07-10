using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

public sealed record RootMissingSurahsResponse(
    IReadOnlyList<MissingSurahItemDto> Surahs);
