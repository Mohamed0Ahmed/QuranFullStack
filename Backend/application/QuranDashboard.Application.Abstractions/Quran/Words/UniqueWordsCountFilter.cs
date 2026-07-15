using QuranDashboard.Application.Abstractions.Common.Filtering;

namespace QuranDashboard.Application.Abstractions.Quran.Words;

/// <summary>
/// Count-range filter for the Unique Words list (Feature 026, US5). Filters unique
/// word identities by exactly the count columns the page already shows:
/// <c>occurrences_count</c>, <c>ayahs_count</c>, <c>surahs_count</c>. Ranges compose
/// (AND) with each other and with search/sort. Predicates run in SQL.
/// </summary>
public sealed record UniqueWordsCountFilter(
    CountRange Occurrences,
    CountRange Ayahs,
    CountRange Surahs)
{
    public static readonly UniqueWordsCountFilter None =
        new(CountRange.Unbounded, CountRange.Unbounded, CountRange.Unbounded);

    public bool IsActive => Occurrences.IsActive || Ayahs.IsActive || Surahs.IsActive;

    public bool IsValid => Occurrences.IsValid && Ayahs.IsValid && Surahs.IsValid;

    public static UniqueWordsCountFilter FromRaw(
        int? occMin, int? occMax,
        int? ayahsMin, int? ayahsMax,
        int? surahsMin, int? surahsMax) =>
        new(
            new CountRange(occMin, occMax),
            new CountRange(ayahsMin, ayahsMax),
            new CountRange(surahsMin, surahsMax));
}
