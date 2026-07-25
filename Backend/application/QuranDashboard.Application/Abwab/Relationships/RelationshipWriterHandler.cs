using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Relationships;

namespace QuranDashboard.Application.Abwab.Relationships;

public sealed class RelationshipWriterHandler(
    IAbwabWriteExecutor executor,
    ICategoryRelationshipStore relationships,
    ICategoryTreeStore categories,
    ISectionWriteStore sections,
    ProtectionResolver protectionResolver,
    IServerClock clock) : IAbwabRelationshipWritePort
{
    private readonly RelationshipEndpointViewBuilder _endpointViews = new(categories, sections);

    public async Task<Guid> AddAsync(AddRelationshipCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var shape = RelationshipShape.From(command.RelationshipType, command.FirstCategoryId, command.SecondCategoryId);
        var relationshipId = Guid.NewGuid();

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (_, operationToken) =>
            {
                await GuardTargetsAsync(shape.EndpointCategoryIds, operationToken);
                await GuardDuplicateAsync(shape, excludeRelationshipId: null, operationToken);
                await GuardCycleAsync(shape, excludeRelationshipId: null, operationToken);

                var relationship = shape.ToNewRow(relationshipId);
                relationships.Add(relationship);

                return await AuditAsync(relationshipId, "added", before: null, after: shape, operationToken);
            });

        await executor.ExecuteAsync(request, cancellationToken);
        return relationshipId;
    }

    public async Task EditAsync(EditRelationshipCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var proposed = RelationshipShape.From(command.RelationshipType, command.FirstCategoryId, command.SecondCategoryId);

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (_, operationToken) =>
            {
                var relationship = await RequireRowAsync(command.CategoryRelationshipId, command.ExpectedVersion, operationToken);
                var current = RelationshipShape.Of(relationship);

                // §7.3: the edit target set is current ∪ proposed, so replacing a protected endpoint
                // with an unprotected one cannot escape the protection that was in effect.
                await GuardTargetsAsync([.. current.EndpointCategoryIds, .. proposed.EndpointCategoryIds], operationToken);

                if (relationship.IsDeleted)
                {
                    throw Stale();
                }

                await GuardDuplicateAsync(proposed, relationship.CategoryRelationshipId, operationToken);
                await GuardCycleAsync(proposed, relationship.CategoryRelationshipId, operationToken);

                proposed.ApplyTo(relationship);

                return await AuditAsync(relationship.CategoryRelationshipId, "edited", current, proposed, operationToken);
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(DeleteRelationshipCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (_, operationToken) =>
            {
                var relationship = await RequireRowAsync(command.CategoryRelationshipId, command.ExpectedVersion, operationToken);
                var stored = RelationshipShape.Of(relationship);

                await GuardTargetsAsync(stored.EndpointCategoryIds, operationToken);

                // Already-deleted maps to row_stale exactly as EditAsync treats it: the caller read a
                // row that no longer supports this action, and answering "تم حذف العلاقة" for a
                // delete that did nothing would report an action that never happened.
                if (relationship.IsDeleted)
                {
                    throw Stale();
                }

                relationship.IsDeleted = true;
                relationship.DeletedAtUtc = clock.UtcNow;

                return await AuditAsync(relationship.CategoryRelationshipId, "deleted", stored, after: null, operationToken);
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    public async Task RestoreAsync(RestoreRelationshipCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (_, operationToken) =>
            {
                var relationship = await RequireRowAsync(command.CategoryRelationshipId, command.ExpectedVersion, operationToken);
                var stored = RelationshipShape.Of(relationship);

                await GuardTargetsAsync(stored.EndpointCategoryIds, operationToken);

                if (!relationship.IsDeleted)
                {
                    throw Stale();
                }

                // Revalidating under this transaction is the only race-safe place to catch a canonical
                // pair/edge that became active again while this row was soft-deleted.
                await GuardDuplicateAsync(stored, relationship.CategoryRelationshipId, operationToken);
                await GuardCycleAsync(stored, relationship.CategoryRelationshipId, operationToken);

                relationship.IsDeleted = false;
                relationship.DeletedAtUtc = null;

                return await AuditAsync(relationship.CategoryRelationshipId, "restored", before: null, after: stored, operationToken);
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    private async Task<CategoryRelationship> RequireRowAsync(Guid relationshipId, uint expectedVersion, CancellationToken cancellationToken)
    {
        var relationship = await relationships.FindTrackedAsync(relationshipId, cancellationToken) ?? throw Stale();
        AbwabRevisionGuards.GuardRowVersion(relationship.Version, expectedVersion);
        return relationship;
    }

    private async Task GuardTargetsAsync(IReadOnlyList<Guid> endpointCategoryIds, CancellationToken cancellationToken)
    {
        await RelationshipProtectionGate.EnsureNoProtectedTargetAsync(protectionResolver, endpointCategoryIds, cancellationToken);

        foreach (var categoryId in endpointCategoryIds.Distinct())
        {
            if (!await categories.ExistsActiveAsync(categoryId, cancellationToken))
            {
                throw new AbwabWriteConflictException(
                    AbwabConflictCodes.CategoryUnavailable, "An endpoint category is unavailable, so the relationship is dormant.");
            }
        }
    }

    private async Task GuardDuplicateAsync(RelationshipShape shape, Guid? excludeRelationshipId, CancellationToken cancellationToken)
    {
        var duplicate = shape.IsDirectional
            ? await relationships.ActiveDirectionalEdgeExistsAsync(
                shape.SourceCategoryId!.Value, shape.TargetCategoryId!.Value, excludeRelationshipId, cancellationToken)
            : await relationships.ActiveMutualPairExistsAsync(
                shape.LowerCategoryId!.Value, shape.HigherCategoryId!.Value, shape.RelationshipType, excludeRelationshipId, cancellationToken);

        if (duplicate)
        {
            throw new AbwabWriteConflictException(
                AbwabConflictCodes.RelationshipDuplicate, "An active relationship already links this pair.");
        }
    }

    // Walks the active broader→narrower graph outward from the proposed narrower endpoint; the edge
    // closes a cycle exactly when the proposed broader endpoint is reachable from it. An explicit
    // direct A→C stays legal alongside A→B→C because only reachability BACK to the broader endpoint
    // is refused.
    private async Task GuardCycleAsync(RelationshipShape shape, Guid? excludeRelationshipId, CancellationToken cancellationToken)
    {
        if (!shape.IsDirectional)
        {
            return;
        }

        var broaderCategoryId = shape.SourceCategoryId!.Value;
        var visited = new HashSet<Guid> { shape.TargetCategoryId!.Value };
        var frontier = new List<Guid> { shape.TargetCategoryId!.Value };

        while (frontier.Count > 0)
        {
            var reached = await relationships.GetActiveDirectionalTargetsAsync(frontier, excludeRelationshipId, cancellationToken);
            if (reached.Contains(broaderCategoryId))
            {
                throw new AbwabWriteConflictException(
                    AbwabConflictCodes.RelationshipCycle, "The proposed edge would close a Broader/Narrower cycle.");
            }

            frontier = [.. reached.Where(visited.Add)];
        }
    }

    private async Task<AbwabAuditedOperationOutcome> AuditAsync(
        Guid relationshipId,
        string action,
        RelationshipShape? before,
        RelationshipShape? after,
        CancellationToken cancellationToken)
    {
        var endpointIds = new List<Guid>();
        if (before is { } beforeShape)
        {
            endpointIds.AddRange(beforeShape.EndpointCategoryIds);
        }

        if (after is { } afterShape)
        {
            endpointIds.AddRange(afterShape.EndpointCategoryIds);
        }

        var views = await _endpointViews.BuildAsync([.. endpointIds.Distinct()], cancellationToken);

        var payload = new RelationshipAuditView(
            relationshipId,
            action,
            before is { } b ? ToStateView(b, views) : null,
            after is { } a ? ToStateView(a, views) : null);

        return AbwabAuditedOperationOutcome.Audited(
            new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize($"relationship.{action}", payload)));
    }

    private static RelationshipStateAuditView ToStateView(
        RelationshipShape shape,
        IReadOnlyDictionary<Guid, RelationshipEndpointAuditView> views)
    {
        var endpoints = shape.EndpointCategoryIds;
        return new RelationshipStateAuditView(
            shape.RelationshipType,
            shape.IsDirectional,
            views[endpoints[0]],
            views[endpoints[1]]);
    }

    private static AbwabWriteConflictException Stale() =>
        new(AbwabConflictCodes.RowStale, "The relationship changed since it was last read.");
}
