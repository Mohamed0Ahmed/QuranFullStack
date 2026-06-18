namespace QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

/// <summary>
/// Lean Mushaf page read model (data-model.md §B1). Carries only what the page
/// view needs: lines, words, page-level navigation context, and division/sajda
/// markers placed by the first-line rule. It MUST NOT include tafsir,
/// translation, full-i3rab, or word morphology.
/// </summary>
public sealed record MushafPageResponse(
    int PageNumber,
    int? PreviousPageNumber,
    int? NextPageNumber,
    IReadOnlyList<SurahOnPage> Surahs,
    AyahRange AyahRange,
    PageNavigationSummary Navigation,
    IReadOnlyList<MushafLineDto> Lines,
    IReadOnlyList<PageMarkerDto> Markers);

public sealed record SurahOnPage(
    int SurahNumber,
    string NameArabic,
    int FirstAyahOnPage,
    int LastAyahOnPage);

public sealed record AyahRange(
    string FirstVerseKey,
    string LastVerseKey);

public sealed record PageNavigationSummary(
    IReadOnlyList<int> JuzNumbers,
    IReadOnlyList<int> HizbNumbers,
    IReadOnlyList<int> RubNumbers);

public sealed record MushafLineDto(
    int LineNumber,
    string LineType,
    bool IsCentered,
    int? SurahNumber,
    IReadOnlyList<MushafWordDto> Words);

public sealed record MushafWordDto(
    string WordLocation,
    string VerseKey,
    int WordNumber,
    int LineWordOrder,
    string TextUthmani,
    bool IsAyahMarker);

public sealed record PageMarkerDto(
    string MarkerType,
    int MarkerNumber,
    string VerseKey,
    int LineNumber,
    string WordLocation,
    string? SajdahType);
