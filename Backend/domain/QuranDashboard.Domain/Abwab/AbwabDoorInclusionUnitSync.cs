namespace QuranDashboard.Domain.Abwab;

public sealed class AbwabDoorInclusionUnitSync
{
    public long Id { get; set; }

    public int DoorInclusionId { get; set; }
    public long SourceUnitId { get; set; }
    public long? TargetUnitId { get; set; }
    public AbwabDoorInclusionSyncState State { get; set; }
    public byte[] SourceFingerprint { get; set; } = [];

    public DateTimeOffset CreatedAtUtc { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int? UpdatedBy { get; set; }
}
