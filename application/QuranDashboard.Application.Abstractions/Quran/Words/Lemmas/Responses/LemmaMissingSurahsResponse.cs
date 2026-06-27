using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

public sealed record LemmaMissingSurahsResponse(
    IReadOnlyList<MissingSurahItemDto> Surahs);
