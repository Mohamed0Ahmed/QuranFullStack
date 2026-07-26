using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Tree;

namespace QuranDashboard.Application.Abwab.Templates;

// Alias add/edit/remove/restore are template.edit-owned internals of the aggregate (§5.2): they
// change no structure, so they carry the row's expected xmin alone and bump no TemplateRevision.
public sealed class TemplateAliasHandler(
    IAbwabWriteExecutor executor,
    IDoorTemplateStore templates,
    ITemplateNodeStore nodes,
    ITemplateNodeAliasStore aliases,
    IServerClock clock)
{
    private readonly TemplateHistory _history = new(nodes, aliases);

    public async Task<Guid> AddAsync(AddTemplateNodeAliasCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var aliasId = Guid.NewGuid();

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (_, operationToken) =>
            {
                var node = await RequireActiveNodeAsync(command.TemplateNodeId, operationToken);
                var (template, working, before) = await LoadAsync(node.DoorTemplateId, operationToken);

                var alias = new TemplateNodeSearchAlias
                {
                    TemplateNodeSearchAliasId = aliasId,
                    TemplateNodeId = node.TemplateNodeId,
                    Value = command.Value,
                    NormalizedValue = ArabicNameNormalizer.Normalize(command.Value),
                };

                aliases.Add(alias);
                working.Aliases.Add(alias);

                return Draft(TemplateAuditActions.AliasAdded, template, before, working, node);
            });

        await executor.ExecuteAsync(request, cancellationToken);
        return aliasId;
    }

    public Task EditAsync(EditTemplateNodeAliasCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return MutateAsync(command.ExpectedTimelineGeneration, command.ActorSubject, command.TemplateNodeSearchAliasId, command.ExpectedVersion, TemplateAuditActions.AliasEdited, alias =>
        {
            if (alias.IsDeleted)
            {
                throw TemplateTreeGuards.Stale();
            }

            alias.Value = command.Value;
            alias.NormalizedValue = ArabicNameNormalizer.Normalize(command.Value);
        }, cancellationToken);
    }

    public Task RemoveAsync(RemoveTemplateNodeAliasCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return MutateAsync(command.ExpectedTimelineGeneration, command.ActorSubject, command.TemplateNodeSearchAliasId, command.ExpectedVersion, TemplateAuditActions.AliasRemoved, alias =>
        {
            if (alias.IsDeleted)
            {
                throw TemplateTreeGuards.Stale();
            }

            alias.IsDeleted = true;
            alias.DeletedAtUtc = clock.UtcNow;
        }, cancellationToken);
    }

    public Task RestoreAsync(RestoreTemplateNodeAliasCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return MutateAsync(command.ExpectedTimelineGeneration, command.ActorSubject, command.TemplateNodeSearchAliasId, command.ExpectedVersion, TemplateAuditActions.AliasRestored, alias =>
        {
            if (!alias.IsDeleted)
            {
                throw TemplateTreeGuards.Stale();
            }

            alias.IsDeleted = false;
            alias.DeletedAtUtc = null;
        }, cancellationToken);
    }

    private async Task MutateAsync(
        ExpectedTimelineGeneration expectedGeneration,
        string actorSubject,
        Guid templateNodeSearchAliasId,
        uint expectedVersion,
        string action,
        Action<TemplateNodeSearchAlias> mutate,
        CancellationToken cancellationToken)
    {
        var request = new AbwabAuditedOperationRequest(
            expectedGeneration,
            actorSubject,
            async (_, operationToken) =>
            {
                var alias = await aliases.FindTrackedAsync(templateNodeSearchAliasId, operationToken) ?? throw TemplateTreeGuards.Stale();
                AbwabRevisionGuards.GuardRowVersion(alias.Version, expectedVersion);

                var node = await RequireActiveNodeAsync(alias.TemplateNodeId, operationToken);
                var (template, working, before) = await LoadAsync(node.DoorTemplateId, operationToken);

                mutate(alias);

                return Draft(action, template, before, working, node);
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    private async Task<TemplateNode> RequireActiveNodeAsync(Guid templateNodeId, CancellationToken cancellationToken)
    {
        var node = await nodes.FindTrackedAsync(templateNodeId, cancellationToken) ?? throw TemplateTreeGuards.Stale();
        return node.IsDeleted ? throw TemplateTreeGuards.Stale() : node;
    }

    private async Task<(DoorTemplate Template, TemplateAggregateWorkingSet Working, TemplateSnapshotAuditView Before)> LoadAsync(
        Guid doorTemplateId,
        CancellationToken cancellationToken)
    {
        var template = await templates.FindTrackedAsync(doorTemplateId, cancellationToken) ?? throw TemplateTreeGuards.Stale();
        if (template.IsDeleted)
        {
            throw TemplateTreeGuards.Stale();
        }

        var working = await _history.LoadAsync(template, cancellationToken);
        return (template, working, working.Snapshot());
    }

    private static AbwabAuditedOperationOutcome Draft(
        string action,
        DoorTemplate template,
        TemplateSnapshotAuditView before,
        TemplateAggregateWorkingSet working,
        TemplateNode node) =>
        TemplateHistory.Draft(
            action,
            new TemplateHistoryAuditView(
                template.DoorTemplateId, before, working.Snapshot(), [TemplateAuditProjections.Node(node, working.Aliases)]));
}
