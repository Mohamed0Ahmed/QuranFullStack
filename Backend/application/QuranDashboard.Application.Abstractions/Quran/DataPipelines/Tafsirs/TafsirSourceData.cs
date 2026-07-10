namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

public sealed record TafsirSourceData(
    IReadOnlyList<TafsirSourceDto> Sources,
    IReadOnlyList<TafsirEntryDto> Entries,
    IReadOnlyList<TafsirAyahEntryDto> AyahEntries,
    IReadOnlyList<ExcludedTafsirSourceDto> ExcludedSources);

public sealed record TafsirSourceDto(
    string SourceKey,
    string LanguageCode,
    string LanguageNameAr,
    string LanguageNameEn,
    string Direction,
    string DisplayNameAr,
    string ShortNameAr,
    string DisplayNameEn,
    string ShortNameEn,
    string? ContributorKey,
    string? ContributorNameAr,
    string? ContributorNameEn,
    string ContributorType,
    string ResourceKind,
    string TafsirKind,
    short ContentCoverageCount,
    string PackageFile,
    string SourceFileOriginal,
    string Sha256,
    long FileSizeBytes,
    string LicenseStatus,
    string ProvenanceStatus,
    string ManifestMetadataJson);

public sealed record TafsirEntryDto(
    string SourceKey,
    string SourceEntryKey,
    int LeaderAyahId,
    string TafsirText,
    short CoveredAyahCount,
    string CoveredAyahKeysJson,
    string SourceShape,
    string TextHash);

public sealed record TafsirAyahEntryDto(
    string SourceKey,
    int AyahId,
    string VerseKey,
    string SourceValueKind,
    string SourceLeaderVerseKey,
    bool IsGroupLeader,
    int SortOrder);

public sealed record ExcludedTafsirSourceDto(
    string SourceKey,
    string Status,
    string ResourceKind,
    int ContentCoverageCount,
    string SourceFileOriginal,
    string ReviewReason);
