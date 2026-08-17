namespace QuranDashboard.Domain.Abwab;

public sealed class AbwabDoorInclusion
{
    public int Id { get; set; }

    public int TargetDoorId { get; set; }
    public int SourceDoorId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public int? DeletedBy { get; set; }

    public uint Version { get; set; }
}
