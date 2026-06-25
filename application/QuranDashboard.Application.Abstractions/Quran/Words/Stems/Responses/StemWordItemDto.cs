namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

/// <summary>
/// Paginated unique-word row scoped to a stem. <see cref="Kind"/> is the
/// requested sub-view (<c>simple</c> or <c>tashkeel</c>); <see cref="DisplayTextUthmani"/>
/// is the stored display text; <see cref="OccurrencesCount"/> is scoped to the stem.
/// </summary>
public sealed record StemWordItemDto(
    int UniqueWordId,
    string Kind,
    string DisplayTextUthmani,
    int OccurrencesCount,
    string FirstVerseKey);
