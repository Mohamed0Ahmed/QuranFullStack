namespace QuranDashboard.Application.Abstractions.Quran.FullI3rab;

public sealed record FullI3rabSourceData(
    IReadOnlyList<FullI3rabSourceDto> Sources,
    IReadOnlyList<FullI3rabEntryDto> Entries,
    IReadOnlyList<FullI3rabAyahEntryDto> AyahEntries,
    IReadOnlyList<string> Warnings);

public sealed record FullI3rabSourceDto(
    string SourceKey,
    string DisplayNameAr,
    string ShortNameAr,
    string DisplayNameEn,
    string ShortNameEn,
    string LanguageCode,
    string Direction,
    string? ContributorNameAr,
    string? ContributorNameEn,
    string ResourceKind,
    string MarkupFormat,
    bool HasQuranQuotationMarkup,
    short ContentCoverageCount,
    string PackageFile,
    string SourceFileOriginal,
    string Sha256,
    long FileSizeBytes,
    string LicenseStatus,
    string ProvenanceStatus,
    string UsageScope,
    string ManifestMetadataJson);

public sealed record FullI3rabEntryDto(
    string SourceKey,
    string SourceEntryKey,
    int LeaderAyahId,
    string I3rabHtml,
    short CoveredAyahCount,
    string CoveredAyahKeysJson,
    string SourceShape,
    string TextHash);

public sealed record FullI3rabAyahEntryDto(
    string SourceKey,
    int AyahId,
    string VerseKey,
    string SourceValueKind,
    string SourceLeaderVerseKey,
    bool IsGroupLeader,
    int SortOrder);
