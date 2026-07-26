using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Tree;

namespace QuranDashboard.Application.Abwab.Templates;

public sealed class TemplateNodeHandler(
    IAbwabWriteExecutor executor,
    IDoorTemplateStore templates,
    ITemplateNodeStore nodes,
    ITemplateNodeAliasStore aliases,
    IServerClock clock)
{
    private readonly TemplateHistory _history = new(nodes, aliases);

    public async Task<Guid> AddAsync(AddTemplateNodeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var templateNodeId = Guid.NewGuid();

        await StructuralAsync(command.ExpectedTimelineGeneration, command.ActorSubject, _ => Task.FromResult(command.DoorTemplateId), command.ExpectedTemplateRevision, TemplateAuditActions.NodeAdded,
            working =>
            {
                // The destination must be ACTIVE, not merely present: the tracked read carries
                // soft-deleted rows too, and a child added under a removed parent would be an
                // unreachable orphan that no restore path and no application recursion can ever see.
                if (command.ParentTemplateNodeId is { } parentId &&
                    !working.Nodes.Any(node => node.TemplateNodeId == parentId && !node.IsDeleted))
                {
                    throw TemplateTreeGuards.Stale();
                }

                var node = new TemplateNode
                {
                    TemplateNodeId = templateNodeId,
                    DoorTemplateId = command.DoorTemplateId,
                    ParentTemplateNodeId = command.ParentTemplateNodeId,
                    Name = command.Name,
                    NormalizedName = ArabicNameNormalizer.Normalize(command.Name),
                    RepresentativeQuranExcerpt = command.RepresentativeQuranExcerpt,
                    Description = command.Description,
                    SiblingOrder = NextSiblingOrder(working, command.ParentTemplateNodeId),
                };

                nodes.Add(node);
                working.Nodes.Add(node);
                return [node];
            },
            cancellationToken);

        return templateNodeId;
    }

    // Content-only: no structure changes, so it carries the row's expected xmin alone and bumps no
    // counter — the §6.4 bump belongs to grouped STRUCTURAL operations.
    public async Task EditAsync(EditTemplateNodeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (_, operationToken) =>
            {
                var node = await RequireActiveNodeAsync(command.TemplateNodeId, command.ExpectedVersion, operationToken);
                var template = await RequireActiveTemplateAsync(node.DoorTemplateId, operationToken);

                var working = await _history.LoadAsync(template, operationToken);
                var before = working.Snapshot();

                node.Name = command.Name;
                node.NormalizedName = ArabicNameNormalizer.Normalize(command.Name);
                node.RepresentativeQuranExcerpt = command.RepresentativeQuranExcerpt;
                node.Description = command.Description;

                return TemplateHistory.Draft(
                    TemplateAuditActions.NodeEdited,
                    new TemplateHistoryAuditView(
                        template.DoorTemplateId, before, working.Snapshot(), [TemplateAuditProjections.Node(node, working.Aliases)]));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    public async Task ReparentAsync(ReparentTemplateNodeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await StructuralAsync(command.ExpectedTimelineGeneration, command.ActorSubject, token => OwningTemplateIdAsync(command.TemplateNodeId, token), command.ExpectedTemplateRevision, TemplateAuditActions.NodeReparented,
            working =>
            {
                var moved = working.Nodes.SingleOrDefault(n => n.TemplateNodeId == command.TemplateNodeId)
                    ?? throw TemplateTreeGuards.Stale();
                AbwabRevisionGuards.GuardRowVersion(moved.Version, command.ExpectedVersion);

                if (moved.IsDeleted)
                {
                    throw TemplateTreeGuards.Stale();
                }

                TemplateTreeGuards.GuardReparentTarget(working.Nodes, moved.TemplateNodeId, command.NewParentTemplateNodeId);

                var previousParentId = moved.ParentTemplateNodeId;
                moved.ParentTemplateNodeId = command.NewParentTemplateNodeId;
                moved.SiblingOrder = NextSiblingOrder(working, command.NewParentTemplateNodeId, excludeTemplateNodeId: moved.TemplateNodeId);

                var touched = CompactSiblingOrders(working, previousParentId, moved.TemplateNodeId);
                return [moved, .. touched];
            },
            cancellationToken);
    }

    public async Task ReorderAsync(ReorderTemplateNodesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await StructuralAsync(command.ExpectedTimelineGeneration, command.ActorSubject, _ => Task.FromResult(command.DoorTemplateId), command.ExpectedTemplateRevision, TemplateAuditActions.NodesReordered,
            working =>
            {
                var siblings = ActiveSiblings(working, command.ParentTemplateNodeId).ToList();
                var requested = command.OrderedTemplateNodeIds;

                // A duplicated id is malformed input that no retry can fix, so it fails as the
                // framework HTTP 400 rather than an abwab.* conflict telling the operator to refresh.
                if (!TemplateNodeOrderRules.IsWellFormedOrder(requested))
                {
                    throw new ArgumentException(
                        "The reorder request must list each sibling exactly once.", nameof(command));
                }

                // A count mismatch or an unknown id IS observed staleness — a sibling was added,
                // removed, or moved under a concurrent write — so refreshing and retrying works.
                if (requested.Count != siblings.Count ||
                    requested.Any(id => siblings.All(sibling => sibling.TemplateNodeId != id)))
                {
                    throw TemplateTreeGuards.Stale();
                }

                // The whole sibling set is rewritten in one pass, so no intermediate state ever holds
                // two nodes on the same order.
                for (var index = 0; index < requested.Count; index++)
                {
                    siblings.Single(sibling => sibling.TemplateNodeId == requested[index]).SiblingOrder = index;
                }

                return siblings;
            },
            cancellationToken);
    }

    public async Task RemoveAsync(RemoveTemplateNodeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await StructuralAsync(command.ExpectedTimelineGeneration, command.ActorSubject, token => OwningTemplateIdAsync(command.TemplateNodeId, token), command.ExpectedTemplateRevision, TemplateAuditActions.NodeRemoved,
            working =>
            {
                var removed = working.Nodes.SingleOrDefault(n => n.TemplateNodeId == command.TemplateNodeId)
                    ?? throw TemplateTreeGuards.Stale();
                AbwabRevisionGuards.GuardRowVersion(removed.Version, command.ExpectedVersion);

                if (removed.IsDeleted)
                {
                    throw TemplateTreeGuards.Stale();
                }

                var subtree = Subtree(working, removed.TemplateNodeId).ToList();
                foreach (var member in subtree)
                {
                    member.IsDeleted = true;
                    member.DeletedAtUtc = clock.UtcNow;
                }

                var touched = CompactSiblingOrders(working, removed.ParentTemplateNodeId, removed.TemplateNodeId);
                return [.. subtree, .. touched];
            },
            cancellationToken);
    }

    // The owning template is resolved INSIDE the operation, never before it: a lookup outside the
    // barrier would attach a pre-transaction row to the change tracker and let identity resolution
    // hand the in-transaction guards those stale values.
    private async Task StructuralAsync(
        ExpectedTimelineGeneration expectedGeneration,
        string actorSubject,
        Func<CancellationToken, Task<Guid>> resolveDoorTemplateId,
        long expectedTemplateRevision,
        string action,
        Func<TemplateAggregateWorkingSet, IReadOnlyList<TemplateNode>> mutate,
        CancellationToken cancellationToken)
    {
        var request = new AbwabAuditedOperationRequest(
            expectedGeneration,
            actorSubject,
            async (_, operationToken) =>
            {
                var doorTemplateId = await resolveDoorTemplateId(operationToken);
                var template = await RequireActiveTemplateAsync(doorTemplateId, operationToken);
                TemplateTreeGuards.GuardTemplateRevision(template, expectedTemplateRevision);

                var working = await _history.LoadAsync(template, operationToken);
                var before = working.Snapshot();

                var changed = mutate(working);

                // One grouped structural operation bumps TemplateRevision exactly once, however many
                // sibling rows it rewrote.
                template.TemplateRevision += 1;

                return TemplateHistory.Draft(
                    action,
                    new TemplateHistoryAuditView(
                        doorTemplateId,
                        before,
                        working.Snapshot(),
                        [.. changed.Distinct().Select(node => TemplateAuditProjections.Node(node, working.Aliases))]));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    private async Task<Guid> OwningTemplateIdAsync(Guid templateNodeId, CancellationToken cancellationToken) =>
        (await nodes.FindTrackedAsync(templateNodeId, cancellationToken) ?? throw TemplateTreeGuards.Stale()).DoorTemplateId;

    private async Task<DoorTemplate> RequireActiveTemplateAsync(Guid doorTemplateId, CancellationToken cancellationToken)
    {
        var template = await templates.FindTrackedAsync(doorTemplateId, cancellationToken) ?? throw TemplateTreeGuards.Stale();
        return template.IsDeleted ? throw TemplateTreeGuards.Stale() : template;
    }

    private async Task<TemplateNode> RequireActiveNodeAsync(Guid templateNodeId, uint expectedVersion, CancellationToken cancellationToken)
    {
        var node = await nodes.FindTrackedAsync(templateNodeId, cancellationToken) ?? throw TemplateTreeGuards.Stale();
        AbwabRevisionGuards.GuardRowVersion(node.Version, expectedVersion);
        return node.IsDeleted ? throw TemplateTreeGuards.Stale() : node;
    }

    private static IEnumerable<TemplateNode> ActiveSiblings(TemplateAggregateWorkingSet working, Guid? parentTemplateNodeId) =>
        working.Nodes
            .Where(node => !node.IsDeleted && node.ParentTemplateNodeId == parentTemplateNodeId)
            .OrderBy(node => node.SiblingOrder);

    private static int NextSiblingOrder(
        TemplateAggregateWorkingSet working,
        Guid? parentTemplateNodeId,
        Guid? excludeTemplateNodeId = null)
    {
        var orders = ActiveSiblings(working, parentTemplateNodeId)
            .Where(node => node.TemplateNodeId != excludeTemplateNodeId)
            .Select(node => node.SiblingOrder)
            .ToList();

        return orders.Count == 0 ? 0 : orders.Max() + 1;
    }

    private static IReadOnlyList<TemplateNode> CompactSiblingOrders(
        TemplateAggregateWorkingSet working,
        Guid? parentTemplateNodeId,
        Guid excludeTemplateNodeId)
    {
        var remaining = ActiveSiblings(working, parentTemplateNodeId)
            .Where(node => node.TemplateNodeId != excludeTemplateNodeId)
            .ToList();

        var touched = new List<TemplateNode>();
        for (var index = 0; index < remaining.Count; index++)
        {
            if (remaining[index].SiblingOrder == index)
            {
                continue;
            }

            remaining[index].SiblingOrder = index;
            touched.Add(remaining[index]);
        }

        return touched;
    }

    private static IEnumerable<TemplateNode> Subtree(TemplateAggregateWorkingSet working, Guid rootTemplateNodeId)
    {
        var members = new List<TemplateNode>();
        var frontier = new List<Guid> { rootTemplateNodeId };

        while (frontier.Count > 0)
        {
            var layer = working.Nodes
                .Where(node => !node.IsDeleted &&
                    (frontier.Contains(node.TemplateNodeId) ||
                        (node.ParentTemplateNodeId is { } parentId && frontier.Contains(parentId))))
                .Where(node => members.All(member => member.TemplateNodeId != node.TemplateNodeId))
                .ToList();

            if (layer.Count == 0)
            {
                break;
            }

            members.AddRange(layer);
            frontier = [.. layer.Select(node => node.TemplateNodeId)];
        }

        return members;
    }
}
