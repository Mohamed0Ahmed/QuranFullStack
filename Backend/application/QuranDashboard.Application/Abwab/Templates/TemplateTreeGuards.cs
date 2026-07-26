using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Templates;

namespace QuranDashboard.Application.Abwab.Templates;

internal static class TemplateTreeGuards
{
    public static void GuardTemplateRevision(DoorTemplate template, long expectedTemplateRevision)
    {
        if (template.TemplateRevision != expectedTemplateRevision)
        {
            throw new AbwabWriteConflictException(
                AbwabConflictCodes.TemplateRevisionStale,
                $"Template revision is stale: expected {expectedTemplateRevision}, current {template.TemplateRevision}.");
        }
    }

    // Walked from the DESTINATION upward over the rows read inside this transaction, so a parent
    // chain another transaction committed in the meantime is validated, not the caller's stale read.
    public static void GuardReparentTarget(
        IReadOnlyList<TemplateNode> templateNodes,
        Guid movedTemplateNodeId,
        Guid? newParentTemplateNodeId)
    {
        if (newParentTemplateNodeId is not { } parentId)
        {
            return;
        }

        if (parentId == movedTemplateNodeId)
        {
            throw Cycle();
        }

        var byId = templateNodes.ToDictionary(node => node.TemplateNodeId);

        // A soft-deleted destination is refused, not merely a missing one: the tracked read carries
        // removed rows, and moving a node under one would strand a live subtree that no restore path
        // and no application recursion can reach.
        if (!byId.TryGetValue(parentId, out var current) || current.IsDeleted)
        {
            throw Stale();
        }

        var visited = new HashSet<Guid>();

        while (true)
        {
            if (!visited.Add(current.TemplateNodeId))
            {
                throw Cycle();
            }

            if (current.TemplateNodeId == movedTemplateNodeId)
            {
                throw Cycle();
            }

            if (current.ParentTemplateNodeId is not { } ancestorId)
            {
                return;
            }

            current = byId.TryGetValue(ancestorId, out var ancestor) ? ancestor : throw Stale();
        }
    }

    public static void GuardAcyclic(IReadOnlyList<TemplateNode> templateNodes)
    {
        if (TemplateParentChainRules.FindCycleNodeId(
                templateNodes.Select(node => (node.TemplateNodeId, node.ParentTemplateNodeId))) is not null)
        {
            throw Cycle();
        }
    }

    public static AbwabWriteConflictException Cycle() =>
        new(AbwabConflictCodes.TemplateCycle, "The requested structure would make the template cyclic.");

    public static AbwabWriteConflictException Stale() =>
        new(AbwabConflictCodes.RowStale, "The template row changed since it was last read.");
}
