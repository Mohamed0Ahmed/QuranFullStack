namespace QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

public sealed record MushafSurahCatalogResponse(
    IReadOnlyList<MushafSurahCatalogItem> Surahs);

public sealed record MushafSurahCatalogItem(
    int SurahNumber,
    string NameArabic,
    int StartPageNumber);
