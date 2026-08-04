namespace QuranDashboard.Domain.Abwab;

public sealed class AbwabDoorRelation
{
    public int Id { get; set; }

    public int DoorAId { get; set; }
    public int DoorBId { get; set; }

    public AbwabRelationType RelationType { get; set; }

    public int? BroaderDoorId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public int? ApprovedBy { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }
    public int? DeletedBy { get; set; }

    public uint Version { get; set; }
}
