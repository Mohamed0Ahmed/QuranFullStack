using QuranDashboard.Domain.Abwab.Templates;

namespace QuranDashboard.Application.Abwab.Templates;

// §6.3 payload types. Template CRUD is rendered in the SEPARATE template-history view and produces
// no main product-audit row, so its action strings all carry the history prefix; only application is
// main-log eligible.
public static class TemplateAuditActions
{
    public const string HistoryPrefix = "template.history.";

    public const string Created = HistoryPrefix + "created";
    public const string Edited = HistoryPrefix + "edited";
    public const string Deleted = HistoryPrefix + "deleted";
    public const string Restored = HistoryPrefix + "restored";
    public const string NodeAdded = HistoryPrefix + "node_added";
    public const string NodeEdited = HistoryPrefix + "node_edited";
    public const string NodeReparented = HistoryPrefix + "node_reparented";
    public const string NodesReordered = HistoryPrefix + "nodes_reordered";
    public const string NodeRemoved = HistoryPrefix + "node_removed";
    public const string AliasAdded = HistoryPrefix + "alias_added";
    public const string AliasEdited = HistoryPrefix + "alias_edited";
    public const string AliasRemoved = HistoryPrefix + "alias_removed";
    public const string AliasRestored = HistoryPrefix + "alias_restored";

    public const string Applied = "template.applied";
}

public sealed record TemplateNodeAuditView(
    Guid TemplateNodeId,
    Guid? ParentTemplateNodeId,
    string Name,
    string? RepresentativeQuranExcerpt,
    string? Description,
    int SiblingOrder,
    bool IsDeleted,
    IReadOnlyList<string> Aliases);

public sealed record TemplateSnapshotAuditView(
    Guid DoorTemplateId,
    string Name,
    string? Description,
    bool IsDeleted,
    IReadOnlyList<TemplateNodeAuditView> Nodes);

public sealed record TemplateFieldChangeAuditView(
    Guid? TemplateNodeId,
    string Field,
    string? Before,
    string? After);

public sealed record TemplateHistoryAuditView(
    Guid DoorTemplateId,
    TemplateSnapshotAuditView? Before,
    TemplateSnapshotAuditView? After,
    IReadOnlyList<TemplateNodeAuditView> ChangedNodes)
{
    // §6.3 requires the changed nodes AND the changed fields. Derived from the two snapshots the
    // operation already carries rather than passed in, so no handler can describe a diff that
    // disagrees with the trees it stored beside it.
    public IReadOnlyList<TemplateFieldChangeAuditView> ChangedFields { get; } =
        TemplateAuditProjections.FieldChanges(Before, After);
}

public sealed record CreatedCategoryAuditView(
    Guid CategoryId,
    string Name,
    string? RepresentativeQuranExcerpt,
    string? Description,
    int Level,
    int SiblingOrder,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<CreatedCategoryAuditView> Children);

public sealed record TemplateLevelCountAuditView(int Level, int Count);

public sealed record TemplateApplicationAuditView(
    Guid DoorTemplateId,
    string TemplateName,
    TemplateSnapshotAuditView TemplateSnapshot,
    Guid TargetCategoryId,
    IReadOnlyList<string> TargetPath,
    IReadOnlyList<CreatedCategoryAuditView> CreatedTree,
    IReadOnlyList<TemplateLevelCountAuditView> CountsByLevel);

internal static class TemplateAuditProjections
{
    public static TemplateSnapshotAuditView Snapshot(
        DoorTemplate template,
        IReadOnlyList<TemplateNode> nodes,
        IReadOnlyList<TemplateNodeSearchAlias> aliases) =>
        new(
            template.DoorTemplateId,
            template.Name,
            template.Description,
            template.IsDeleted,
            [.. nodes.OrderBy(node => node.SiblingOrder).ThenBy(node => node.Name, StringComparer.Ordinal)
                .Select(node => Node(node, aliases))]);

    public static IReadOnlyList<TemplateFieldChangeAuditView> FieldChanges(
        TemplateSnapshotAuditView? before,
        TemplateSnapshotAuditView? after)
    {
        var changes = new List<TemplateFieldChangeAuditView>();
        Add(changes, null, "Name", before?.Name, after?.Name);
        Add(changes, null, "Description", before?.Description, after?.Description);
        Add(changes, null, "IsDeleted", Flag(before?.IsDeleted), Flag(after?.IsDeleted));

        var beforeNodes = (before?.Nodes ?? []).ToDictionary(node => node.TemplateNodeId);
        var afterNodes = (after?.Nodes ?? []).ToDictionary(node => node.TemplateNodeId);
        var nodeIds = (after?.Nodes ?? []).Select(node => node.TemplateNodeId)
            .Concat((before?.Nodes ?? []).Select(node => node.TemplateNodeId))
            .Distinct();

        foreach (var nodeId in nodeIds)
        {
            var left = beforeNodes.GetValueOrDefault(nodeId);
            var right = afterNodes.GetValueOrDefault(nodeId);

            Add(changes, nodeId, "Name", left?.Name, right?.Name);
            Add(changes, nodeId, "RepresentativeQuranExcerpt", left?.RepresentativeQuranExcerpt, right?.RepresentativeQuranExcerpt);
            Add(changes, nodeId, "Description", left?.Description, right?.Description);
            Add(changes, nodeId, "ParentTemplateNodeId", left?.ParentTemplateNodeId?.ToString(), right?.ParentTemplateNodeId?.ToString());
            Add(changes, nodeId, "SiblingOrder", left?.SiblingOrder.ToString(), right?.SiblingOrder.ToString());
            Add(changes, nodeId, "Aliases", Joined(left?.Aliases), Joined(right?.Aliases));
            Add(changes, nodeId, "IsDeleted", Flag(left?.IsDeleted), Flag(right?.IsDeleted));
        }

        return changes;
    }

    public static TemplateNodeAuditView Node(TemplateNode node, IReadOnlyList<TemplateNodeSearchAlias> aliases) =>
        new(
            node.TemplateNodeId,
            node.ParentTemplateNodeId,
            node.Name,
            node.RepresentativeQuranExcerpt,
            node.Description,
            node.SiblingOrder,
            node.IsDeleted,
            [.. aliases.Where(alias => alias.TemplateNodeId == node.TemplateNodeId && !alias.IsDeleted).Select(alias => alias.Value)]);

    private static void Add(List<TemplateFieldChangeAuditView> changes, Guid? templateNodeId, string field, string? before, string? after)
    {
        if (!string.Equals(before, after, StringComparison.Ordinal))
        {
            changes.Add(new TemplateFieldChangeAuditView(templateNodeId, field, before, after));
        }
    }

    private static string? Flag(bool? value) => value?.ToString();

    private static string? Joined(IReadOnlyList<string>? values) => values is null ? null : string.Join('،', values);
}
