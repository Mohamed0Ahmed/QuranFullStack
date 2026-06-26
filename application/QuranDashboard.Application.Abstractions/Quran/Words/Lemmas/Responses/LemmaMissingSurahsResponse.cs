using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

/// <summary>
/// Missing surahs for a lemma — the complement of mentioned surahs against the
/// authoritative 114-surah catalogue. Reuses the shared <see cref="MissingSurahItemDto"/>.
/// </summary>
public sealed record LemmaMissingSurahsResponse(
    int Id,
    string LemmaText,
    int MissingSurahsCount,
    IReadOnlyList<MissingSurahItemDto> Surahs);
