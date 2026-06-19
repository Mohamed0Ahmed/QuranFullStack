namespace QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

/// <summary>
/// Study-area source catalog: all tafsir, translation, and full-i3rab sources
/// for selector UI. Metadata only — no ayah text.
/// </summary>
public sealed record MushafStudySourceCatalogResponse(
    IReadOnlyList<StudySourceCatalogItem> TafsirSources,
    IReadOnlyList<StudySourceCatalogItem> TranslationSources,
    IReadOnlyList<StudySourceCatalogItem> FullI3rabSources);

/// <summary>
/// One selectable study source row from a dimension table.
/// Kind-specific fields (<see cref="TafsirKind"/>, <see cref="TranslationType"/>) are
/// null when not applicable to that catalog list.
/// </summary>
public sealed record StudySourceCatalogItem(
    string SourceKey,
    string DisplayNameAr,
    string? DisplayNameEn,
    string LanguageCode,
    string? LanguageNameAr,
    string Direction,
    string? TafsirKind,
    string? TranslationType);
