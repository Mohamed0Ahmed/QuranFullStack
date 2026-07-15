using QuranDashboard.Application.Abstractions.Common.Filtering;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots;

/// <summary>
/// Count-range filter for the Roots list (Feature 026, US5). Filters root dimension
/// entries by exactly the seven count columns the page already shows. Predicates run
/// in memory over the cached whole-summary rows (no backend cache-key change).
/// </summary>
public sealed record RootsCountFilter(
    CountRange Occurrences,
    CountRange Ayahs,
    CountRange Surahs,
    CountRange SimpleWords,
    CountRange TashkeelWords,
    CountRange Lemmas,
    CountRange Stems)
{
    public static readonly RootsCountFilter None = new(
        CountRange.Unbounded, CountRange.Unbounded, CountRange.Unbounded,
        CountRange.Unbounded, CountRange.Unbounded, CountRange.Unbounded, CountRange.Unbounded);

    public bool IsActive =>
        Occurrences.IsActive || Ayahs.IsActive || Surahs.IsActive
        || SimpleWords.IsActive || TashkeelWords.IsActive || Lemmas.IsActive || Stems.IsActive;

    public bool IsValid =>
        Occurrences.IsValid && Ayahs.IsValid && Surahs.IsValid
        && SimpleWords.IsValid && TashkeelWords.IsValid && Lemmas.IsValid && Stems.IsValid;

    public static RootsCountFilter FromRaw(
        int? occMin, int? occMax,
        int? ayahsMin, int? ayahsMax,
        int? surahsMin, int? surahsMax,
        int? simpleWordsMin, int? simpleWordsMax,
        int? tashkeelWordsMin, int? tashkeelWordsMax,
        int? lemmasMin, int? lemmasMax,
        int? stemsMin, int? stemsMax) =>
        new(
            new CountRange(occMin, occMax),
            new CountRange(ayahsMin, ayahsMax),
            new CountRange(surahsMin, surahsMax),
            new CountRange(simpleWordsMin, simpleWordsMax),
            new CountRange(tashkeelWordsMin, tashkeelWordsMax),
            new CountRange(lemmasMin, lemmasMax),
            new CountRange(stemsMin, stemsMax));
}
