namespace QuranDashboard.Domain.Quran.PhraseSearch;

public sealed class PhraseIndexState
{
    public const short SingletonId = 1;

    public short Id { get; set; }
    public long SourceRevision { get; set; }
    public string? SourceFingerprint { get; set; }
    public Guid? ActiveBuildId { get; set; }
    public Guid? PreviousBuildId { get; set; }
    public bool IsStale { get; set; }
    public string? StaleReason { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
