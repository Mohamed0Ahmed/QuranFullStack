namespace QuranDashboard.Domain.Quran.FullI3rab;

public sealed class FullI3rabSource
{
    public int Id { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public string DisplayNameAr { get; set; } = string.Empty;
    public string ShortNameAr { get; set; } = string.Empty;
    public string DisplayNameEn { get; set; } = string.Empty;
    public string ShortNameEn { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string? ContributorNameAr { get; set; }
    public string? ContributorNameEn { get; set; }
    public string ResourceKind { get; set; } = string.Empty;
    public string MarkupFormat { get; set; } = string.Empty;
    public bool HasQuranQuotationMarkup { get; set; }
    public short ContentCoverageCount { get; set; }
    public string PackageFile { get; set; } = string.Empty;
    public string SourceFileOriginal { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string LicenseStatus { get; set; } = string.Empty;
    public string ProvenanceStatus { get; set; } = string.Empty;
    public string UsageScope { get; set; } = string.Empty;
    public string? ManifestMetadata { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
}
