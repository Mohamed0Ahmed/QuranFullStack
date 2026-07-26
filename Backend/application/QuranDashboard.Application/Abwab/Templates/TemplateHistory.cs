using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Templates;

namespace QuranDashboard.Application.Abwab.Templates;

// Builds the §6.3 template-history payload from ONE in-transaction read of the aggregate. The
// before/after trees are projected from the same in-memory node list the operation mutates, because
// re-querying after the mutation would miss rows that are added but not yet saved.
internal sealed class TemplateHistory(ITemplateNodeStore nodes, ITemplateNodeAliasStore aliases)
{
    public async Task<TemplateAggregateWorkingSet> LoadAsync(DoorTemplate template, CancellationToken cancellationToken)
    {
        var templateNodes = await nodes.FindTrackedForTemplateAsync(template.DoorTemplateId, cancellationToken);
        var nodeAliases = await aliases.FindTrackedForNodesAsync(
            [.. templateNodes.Select(node => node.TemplateNodeId)], cancellationToken);

        return new TemplateAggregateWorkingSet(template, [.. templateNodes], [.. nodeAliases]);
    }

    public static AbwabAuditedOperationOutcome Draft(string action, TemplateHistoryAuditView view) =>
        AbwabAuditedOperationOutcome.Audited(new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize(action, view)));
}

internal sealed record TemplateAggregateWorkingSet(
    DoorTemplate Template,
    List<TemplateNode> Nodes,
    List<TemplateNodeSearchAlias> Aliases)
{
    public TemplateSnapshotAuditView Snapshot() => TemplateAuditProjections.Snapshot(Template, Nodes, Aliases);
}
