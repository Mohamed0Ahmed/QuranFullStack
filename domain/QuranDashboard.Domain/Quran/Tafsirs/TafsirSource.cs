namespace QuranDashboard.Domain.Quran.Tafsirs;

public sealed class TafsirSource
{
    public int Id { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageNameAr { get; set; } = string.Empty;
    public string LanguageNameEn { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string DisplayNameAr { get; set; } = string.Empty;
    public string ShortNameAr { get; set; } = string.Empty;
    public string DisplayNameEn { get; set; } = string.Empty;
    public string ShortNameEn { get; set; } = string.Empty;
    public string? ContributorKey { get; set; }
    public string? ContributorNameAr { get; set; }
    public string? ContributorNameEn { get; set; }
    public string ContributorType { get; set; } = string.Empty;
    public string ResourceKind { get; set; } = string.Empty;
    public string TafsirKind { get; set; } = string.Empty;
    public short ContentCoverageCount { get; set; }
    public string PackageFile { get; set; } = string.Empty;
    public string SourceFileOriginal { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string LicenseStatus { get; set; } = string.Empty;
    public string ProvenanceStatus { get; set; } = string.Empty;
    public string? ManifestMetadata { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
}
