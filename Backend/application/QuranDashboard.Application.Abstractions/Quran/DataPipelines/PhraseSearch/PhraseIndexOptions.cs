namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

public sealed class PhraseIndexOptions
{
    public const string SectionName = "PhraseSearch";
    public const string OperatorStorageProofContract = "operator-verified-database-filesystem-v1";

    public int RequestTimeoutSeconds { get; set; } = 10;
    public int FailedBuildRetentionDays { get; set; } = 30;
    public long DiskSafetyBytes { get; set; } = 4L * 1024 * 1024 * 1024;
    public long? VerifiedDatabaseFreeBytes { get; set; }
    public string? DatabaseStorageProofContract { get; set; }
}
