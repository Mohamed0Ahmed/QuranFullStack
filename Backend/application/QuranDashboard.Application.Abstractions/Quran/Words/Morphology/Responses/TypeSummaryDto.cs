namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;

/// <summary>
/// Controlled POS summary entry shared by the Lemmas and Stems explorers
/// (Feature 016). <see cref="Code"/> and <see cref="ArabicLabel"/> are existing
/// controlled POS values; never invented. Ordered by <see cref="OccurrencesCount"/>
/// descending, then earliest Mushaf occurrence ascending.
/// </summary>
public sealed record TypeSummaryDto(
    string Code,
    string ArabicLabel,
    int OccurrencesCount);
