namespace QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

public sealed record MushafStudySourceCatalogResponse(
    IReadOnlyList<StudySourceCatalogItem> TafsirSources,
    IReadOnlyList<StudySourceCatalogItem> TranslationSources,
    IReadOnlyList<StudySourceCatalogItem> FullI3rabSources);

public sealed record StudySourceCatalogItem(
    string SourceKey,
    string DisplayNameAr,
    string? DisplayNameEn,
    string LanguageCode,
    string? LanguageNameAr,
    string Direction,
    string? TafsirKind,
    string? TranslationType);
