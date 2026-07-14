using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

public sealed record StemMissingSurahsResponse(
    IReadOnlyList<MissingSurahItemDto> Surahs);
