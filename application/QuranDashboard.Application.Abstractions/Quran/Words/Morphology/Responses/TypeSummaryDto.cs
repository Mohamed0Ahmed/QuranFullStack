namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;

/// <summary>
/// Controlled POS summary entry shared by the Lemmas and Stems explorers
/// (Feature 016). <see cref="Code"/> and labels are existing controlled POS
/// values; never invented. Ordered by <see cref="OccurrencesCount"/> descending,
/// then earliest Mushaf occurrence ascending — the first item is the dominant type.
/// </summary>
public sealed record TypeSummaryDto(
    string Code,
    string ArabicLabel,
    string EnglishLabel,
    int OccurrencesCount,
    int FirstSurahNumber,
    int FirstAyahNumber,
    int FirstWordNumber);
