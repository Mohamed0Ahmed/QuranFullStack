namespace QuranDashboard.Domain.Abwab;

public sealed class AbwabDoorRelation
{
    public int Id { get; set; }

    // Canonically ordered — DoorAId < DoorBId, enforced by a CHECK, for ALL three types. That one
    // rule carries three guarantees at once: no self-relation, one row per pair (deleting from
    // either side deletes it), and no second row for the opposite direction of the same pair.
    public int DoorAId { get; set; }
    public int DoorBId { get; set; }

    public AbwabRelationType RelationType { get; set; }

    // The endpoint that is MORE comprehensive — the whole of this feature's direction semantics.
    // NOT NULL exactly for Comprehensiveness, and always DoorAId or DoorBId (both CHECK-enforced).
    // Direction lives here rather than in column position because the pair is canonically ordered.
    public int? BroaderDoorId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public int? ApprovedBy { get; set; }

    // Explicit deletion is soft, like every other abwab row. Dormancy — a relation hidden because
    // an ENDPOINT DOOR is archived — is a different thing entirely and is derived at read time,
    // never stored here.
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public int? DeletedBy { get; set; }

    // Bound to Postgres's xmin system column (AbwabDoorRelationConfiguration) — never assigned in
    // code. Nothing consumes it: relation writes carry no version token, since they move no door's
    // xmin and add/delete address rows by id (plan §5.4). Diagnostics only.
    public uint Version { get; set; }
}
