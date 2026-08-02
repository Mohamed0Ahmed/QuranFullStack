namespace QuranDashboard.Domain.Abwab;

public sealed class AbwabDoor
{
    public int Id { get; set; }

    // Every door belongs to a section, including a root one — «كل الأبواب» is the view across all
    // sections, not a home for doors that have none. Root scope is about having no PARENT (D6), which is
    // ParentId's business and independent of this. NOT NULL in the database too: the write side refuses a
    // section-less door, and the column refuses one the write side ever lets through.
    public int SectionId { get; set; }
    public int? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Free text only — never an FK or a verified Quran reference.
    public string? RepresentativeAyahText { get; set; }

    public int OrderValue { get; set; }

    // NULL everywhere except live root doors: GlobalOrderValue IS NOT NULL ⟺
    // (ParentId IS NULL AND DeletedAtUtc IS NULL). Independent of OrderValue — the superset's
    // own ordering space, never touched by a Section-scoped reorder (plan §5).
    public int? GlobalOrderValue { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public int? DeletedBy { get; set; }

    // Bound to Postgres's xmin system column (AbwabDoorConfiguration) — never assigned in code.
    public uint Version { get; set; }
}
