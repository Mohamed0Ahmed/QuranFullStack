namespace QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

/// <summary>
/// Surah jump catalog: each surah's Arabic name and the Mushaf page where it begins.
/// </summary>
public sealed record MushafSurahCatalogResponse(
    IReadOnlyList<MushafSurahCatalogItem> Surahs);

public sealed record MushafSurahCatalogItem(
    int SurahNumber,
    string NameArabic,
    int StartPageNumber);
