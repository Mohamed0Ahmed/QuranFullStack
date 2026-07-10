namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

/// <summary>
/// Paginated unique-word row scoped to a stem. <see cref="DisplayText"/> is the
/// stored display text; <see cref="OccurrencesCount"/> is scoped to the stem.
/// </summary>
public sealed record StemWordItemDto(
    int UniqueWordId,
    string DisplayText,
    int OccurrencesCount);
