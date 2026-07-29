namespace QuranDashboard.Domain.Abwab;

public sealed class AbwabTemplateNode
{
    public int Id { get; set; }

    public int TemplateId { get; set; }

    // NULL for the template's root, and exactly one live row per template may hold NULL — the
    // invariant the deep copy rests on, since "the template root becomes a new child of each
    // target" is undefined with two roots.
    public int? ParentNodeId { get; set; }

    // The same four authoring fields a door carries, because a template IS a door subtree and its
    // nodes are authored through the same form.
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Free text only — never an FK or a verified Quran reference, exactly as on AbwabDoor.
    public string? RepresentativeAyahText { get; set; }

    // A text[] column, not a child table: the two-table decision forecloses one, and template
    // aliases are never searched, soft-deleted, or individually identified. ALWAYS assign a new
    // array — EF compares this by reference, so an in-place mutation can go undetected.
    public IReadOnlyList<string> Aliases { get; set; } = [];

    // 1..N within (TemplateId, ParentNodeId). The copy carries it through verbatim, which is what
    // preserves sibling order in every created subtree.
    public int OrderValue { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public int? ApprovedBy { get; set; }

    // Deleting a node soft-deletes its whole subtree — a template child has no meaning without its
    // parent — and the root cannot be deleted at all; deleting the template is the way.
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public int? DeletedBy { get; set; }

    // Bound to Postgres's xmin system column (AbwabTemplateNodeConfiguration) — never assigned in
    // code, never consumed (plan §5.4).
    public uint Version { get; set; }
}
