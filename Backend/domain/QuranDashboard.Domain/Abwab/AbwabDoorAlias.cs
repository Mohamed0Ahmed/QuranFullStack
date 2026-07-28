namespace QuranDashboard.Domain.Abwab;

public sealed class AbwabDoorAlias
{
    public int Id { get; set; }
    public int DoorId { get; set; }
    public string Value { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public int? DeletedBy { get; set; }
}
