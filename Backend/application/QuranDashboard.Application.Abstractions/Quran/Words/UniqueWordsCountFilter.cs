using QuranDashboard.Application.Abstractions.Common.Filtering;

namespace QuranDashboard.Application.Abstractions.Quran.Words;

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
