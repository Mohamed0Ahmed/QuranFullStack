using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

/// <summary>
/// Missing surahs for a stem — the complement of mentioned surahs against the
/// authoritative 114-surah catalogue.
/// </summary>
public sealed record StemMissingSurahsResponse(
    IReadOnlyList<MissingSurahItemDto> Surahs);
