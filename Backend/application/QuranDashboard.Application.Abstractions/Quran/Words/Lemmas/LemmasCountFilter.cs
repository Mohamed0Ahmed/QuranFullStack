using QuranDashboard.Application.Abstractions.Common.Filtering;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

public sealed record LemmasCountFilter(
    CountRange Occurrences,
    CountRange Ayahs,
    CountRange Surahs,
    CountRange SimpleWords,
    CountRange TashkeelWords,
    CountRange Stems)
{
    public static readonly LemmasCountFilter None = new(
        CountRange.Unbounded, CountRange.Unbounded, CountRange.Unbounded,
        CountRange.Unbounded, CountRange.Unbounded, CountRange.Unbounded);

    public bool IsActive =>
        Occurrences.IsActive || Ayahs.IsActive || Surahs.IsActive
        || SimpleWords.IsActive || TashkeelWords.IsActive || Stems.IsActive;

    public bool IsValid =>
        Occurrences.IsValid && Ayahs.IsValid && Surahs.IsValid
        && SimpleWords.IsValid && TashkeelWords.IsValid && Stems.IsValid;

    public static LemmasCountFilter FromRaw(
        int? occMin, int? occMax,
        int? ayahsMin, int? ayahsMax,
        int? surahsMin, int? surahsMax,
        int? simpleWordsMin, int? simpleWordsMax,
        int? tashkeelWordsMin, int? tashkeelWordsMax,
        int? stemsMin, int? stemsMax) =>
        new(
            new CountRange(occMin, occMax),
            new CountRange(ayahsMin, ayahsMax),
            new CountRange(surahsMin, surahsMax),
            new CountRange(simpleWordsMin, simpleWordsMax),
            new CountRange(tashkeelWordsMin, tashkeelWordsMax),
            new CountRange(stemsMin, stemsMax));
}
