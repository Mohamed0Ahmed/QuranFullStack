namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

/// <summary>
/// Paginated unique-word row scoped to a lemma. <see cref="Kind"/> is the
/// requested sub-view (<c>simple</c> or <c>tashkeel</c>); <see cref="DisplayTextUthmani"/>
/// is the stored display text; <see cref="OccurrencesCount"/> is scoped to the lemma.
/// </summary>
public sealed record LemmaWordItemDto(
    int UniqueWordId,
    string Kind,
    string DisplayTextUthmani,
    int OccurrencesCount,
    string FirstVerseKey);
