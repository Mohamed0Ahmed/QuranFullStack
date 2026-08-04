namespace QuranDashboard.Domain.Abwab;

public sealed class AbwabTemplateNode
{
    public int Id { get; set; }

    public int TemplateId { get; set; }

    public int? ParentNodeId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Free text only — never an FK or a verified Quran reference, exactly as on AbwabDoor.
    public string? RepresentativeAyahText { get; set; }

    // A text[] column, not a child table: the two-table decision forecloses one, and template
    // aliases are never searched, soft-deleted, or individually identified. ALWAYS assign a new
    // array — EF compares this by reference, so an in-place mutation can go undetected.
    public IReadOnlyList<string> Aliases { get; set; } = [];

    public int OrderValue { get; set; }

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
