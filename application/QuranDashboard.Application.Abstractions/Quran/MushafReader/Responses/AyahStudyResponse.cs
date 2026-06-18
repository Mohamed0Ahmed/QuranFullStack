namespace QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

/// <summary>
/// Ayah study read model (data-model.md §B2): the core ayah identity plus the
/// three selected sources (tafsir, translation, full i3rab) loaded TOGETHER in
/// v1. A source kind whose key resolved to null is represented by a null block
/// and a per-kind empty state (never a silent substitution).
/// </summary>
public sealed record AyahStudyResponse(
    AyahCoreDto Ayah,
    SelectedSourcesDto SelectedSources,
    TafsirEntryDto? Tafsir,
    TranslationEntryDto? Translation,
    FullI3rabEntryDto? FullI3rab);

public sealed record AyahCoreDto(
    string VerseKey,
    int SurahNumber,
    string SurahNameArabic,
    int AyahNumber,
    string TextUthmani,
    int WordsCount,
    int PageFrom,
    int PageTo,
    int JuzNumber,
    int HizbNumber,
    int RubNumber,
    SajdaDto? Sajda);

public sealed record SajdaDto(
    int SajdahNumber,
    string VerseKey,
    string SajdahType);

public sealed record SelectedSourcesDto(
    string? TafsirSource,
    string? TranslationSource,
    string? FullI3rabSource);

public sealed record TafsirEntryDto(
    string SourceKey,
    string DisplayNameAr,
    string? ShortNameAr,
    string LanguageCode,
    string Direction,
    string TafsirKind,
    string SourceValueKind,
    string? SourceLeaderVerseKey,
    bool IsGroupLeader,
    int CoveredAyahCount,
    IReadOnlyList<string> CoveredAyahKeys,
    string Text);

public sealed record TranslationEntryDto(
    string SourceKey,
    string? DisplayNameAr,
    string? DisplayNameEn,
    string LanguageCode,
    string Direction,
    string TranslationType,
    bool ContainsHtmlMarkup,
    string Text);

public sealed record FullI3rabEntryDto(
    string SourceKey,
    string DisplayNameAr,
    string? ShortNameAr,
    string MarkupFormat,
    string SourceValueKind,
    string? SourceLeaderVerseKey,
    bool IsGroupLeader,
    int CoveredAyahCount,
    IReadOnlyList<string> CoveredAyahKeys,
    string Html);
