namespace QuranDashboard.Domain.Abwab;

public sealed class AbwabTemplate
{
    public int Id { get; set; }

    // No Name: a template's name IS its root node's name (the node with ParentNodeId == null, one
    // per template by a partial unique index). A column here would be a second edit path for one
    // string and would drift the moment the root is edited through the authoring modal.

    public DateTimeOffset CreatedAtUtc { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public int? ApprovedBy { get; set; }

    // Deleting a template soft-deletes THIS row only — the reader filters by it, so cascading into
    // the node rows would be ceremony. There is no restore route and no archived-templates view.
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public int? DeletedBy { get; set; }

    // Bound to Postgres's xmin system column (AbwabTemplateConfiguration) — never assigned in code.
    // Nothing consumes it: no templates route carries a version token (plan §5.4).
    public uint Version { get; set; }
}
