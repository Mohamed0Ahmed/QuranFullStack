using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

/// <summary>
/// Missing surahs for a stem — the complement of mentioned surahs against the
/// authoritative 114-surah catalogue. Reuses the shared <see cref="MissingSurahItemDto"/>.
/// </summary>
public sealed record StemMissingSurahsResponse(
    int Id,
    string StemText,
    int MissingSurahsCount,
    IReadOnlyList<MissingSurahItemDto> Surahs);
