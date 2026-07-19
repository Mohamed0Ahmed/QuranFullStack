using QuranDashboard.Application.Abstractions.Common.Filtering;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems;

// Predicates run in memory over the cached whole-summary rows (no backend cache-key change).
public sealed record StemsCountFilter(
    CountRange Occurrences,
    CountRange Ayahs,
    CountRange Surahs,
    CountRange SimpleWords,
    CountRange TashkeelWords)
{
    public static readonly StemsCountFilter None = new(
        CountRange.Unbounded, CountRange.Unbounded, CountRange.Unbounded,
        CountRange.Unbounded, CountRange.Unbounded);

    public bool IsActive =>
        Occurrences.IsActive || Ayahs.IsActive || Surahs.IsActive
        || SimpleWords.IsActive || TashkeelWords.IsActive;

    public bool IsValid =>
        Occurrences.IsValid && Ayahs.IsValid && Surahs.IsValid
        && SimpleWords.IsValid && TashkeelWords.IsValid;

    public static StemsCountFilter FromRaw(
        int? occMin, int? occMax,
        int? ayahsMin, int? ayahsMax,
        int? surahsMin, int? surahsMax,
        int? simpleWordsMin, int? simpleWordsMax,
        int? tashkeelWordsMin, int? tashkeelWordsMax) =>
        new(
            new CountRange(occMin, occMax),
            new CountRange(ayahsMin, ayahsMax),
            new CountRange(surahsMin, surahsMax),
            new CountRange(simpleWordsMin, simpleWordsMax),
            new CountRange(tashkeelWordsMin, tashkeelWordsMax));
}
