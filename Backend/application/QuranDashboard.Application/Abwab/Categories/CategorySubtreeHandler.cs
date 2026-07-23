using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abwab.Categories;

// T055: atomic subtree soft-delete + parent-first operation-restore. One DeletionOperationId and one
// TreeRevision bump per atomic operation; Deletion protection on every affected category, InternalStructure
// on the surviving/restored parent; deterministic (CategoryId-ordered) processing; the reservation seam is
// checked but stays inert in 029 (T047/T051); dependent dormancy is a generic RESTRICT/no-cascade property
// of the schema, proven only by the core test fixture (no relationship/link schema here).
public sealed class CategorySubtreeHandler(
    IAbwabWriteExecutor executor,
    ICategoryTreeStore categories,
    ISectionWriteStore sections,
    ProtectionResolver protectionResolver,
    IDeletionReservationChecker reservationChecker,
    IServerClock clock)
{
    public async Task<Guid> DeleteAsync(SubtreeDeleteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var deletionOperationId = Guid.NewGuid();

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (revision, operationToken) =>
            {
                AbwabRevisionGuards.GuardTreeRevision(revision, command.ExpectedTreeRevision);

                var root = await categories.FindTrackedAsync(command.CategoryId, includeDeleted: false, operationToken)
                    ?? throw Unavailable();

                AbwabRevisionGuards.GuardRowVersion(root.Version, command.ExpectedVersion);

                var descendants = await categories.FindDescendantsAsync(root.CategoryId, operationToken);

                var affected = new List<Category> { root };
                affected.AddRange(descendants);
                affected = [.. affected.OrderBy(c => c.CategoryId)];
                var affectedIds = affected.Select(c => c.CategoryId).ToList();

                if (await reservationChecker.IsReservedByPendingAsync(affectedIds, operationToken))
                {
                    throw new AbwabWriteConflictException(AbwabConflictCodes.CategoryReservedByPending, "One or more categories are reserved by a pending request.");
                }

                foreach (var category in affected)
                {
                    await CategoryProtectionGate.EnsureNotManuallyProtectedAsync(
                        protectionResolver, category.CategoryId, ManualProtectionType.Deletion, operationToken);
                }

                if (root.ParentCategoryId is { } parentId)
                {
                    await CategoryProtectionGate.EnsureNotManuallyProtectedAsync(
                        protectionResolver, parentId, ManualProtectionType.InternalStructure, operationToken);
                }

                var now = clock.UtcNow;
                foreach (var category in affected)
                {
                    category.IsDeleted = true;
                    category.DeletedAtUtc = now;
                    category.DeletionOperationId = deletionOperationId;
                }

                revision.TreeRevision += 1;

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("category.subtree_deleted", new
                    {
                        deletionOperationId,
                        rootCategoryId = root.CategoryId,
                        affectedCategoryIds = affectedIds,
                    })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
        return deletionOperationId;
    }

    public async Task RestoreAsync(OperationRestoreCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (revision, operationToken) =>
            {
                AbwabRevisionGuards.GuardTreeRevision(revision, command.ExpectedTreeRevision);

                var toRestore = await categories.FindByDeletionOperationAsync(command.DeletionOperationId, operationToken);
                if (toRestore.Count == 0)
                {
                    throw Unavailable();
                }

                var restoredIds = toRestore.Select(c => c.CategoryId).ToHashSet();

                foreach (var category in toRestore)
                {
                    // Restore revalidates Deletion (in addition to InternalStructure on the parent below) —
                    // the resolver still addresses a soft-deleted category by immutable CategoryId, so an
                    // inherited Deletion grant applied to a still-active ancestor AFTER the original delete
                    // must block bringing this row back too, mirroring the delete-side check symmetrically.
                    await CategoryProtectionGate.EnsureNotManuallyProtectedAsync(
                        protectionResolver, category.CategoryId, ManualProtectionType.Deletion, operationToken);

                    if (category.ParentCategoryId is { } parentId)
                    {
                        var parentActive = restoredIds.Contains(parentId) || await categories.ExistsActiveAsync(parentId, operationToken);
                        if (!parentActive)
                        {
                            throw Unavailable();
                        }

                        await CategoryProtectionGate.EnsureNotManuallyProtectedAsync(
                            protectionResolver, parentId, ManualProtectionType.InternalStructure, operationToken);

                        await CategoryTreeGuards.GuardNameConflictAsync(categories, category.NormalizedName, parentId, category.CategoryId, operationToken);

                        var siblingConflict = category.SiblingOrder is null ||
                            await categories.HasActiveSiblingWithOrderAsync(parentId, category.SiblingOrder.Value, category.CategoryId, operationToken);
                        if (siblingConflict)
                        {
                            var maxSibling = await categories.GetMaxSiblingOrderAsync(parentId, category.CategoryId, operationToken) ?? -1;
                            category.SiblingOrder = maxSibling + 1;
                        }
                    }
                    else
                    {
                        var sectionId = category.SectionId ?? throw Unavailable();
                        if (!await sections.ExistsActiveAsync(sectionId, operationToken))
                        {
                            throw Unavailable();
                        }

                        await CategoryTreeGuards.GuardNameConflictAsync(categories, category.NormalizedName, null, category.CategoryId, operationToken);

                        if (category.SectionOrder is null ||
                            await categories.HasActiveSectionRootWithOrderAsync(sectionId, category.SectionOrder.Value, category.CategoryId, operationToken))
                        {
                            var maxSectionOrder = await categories.GetMaxSectionOrderAsync(sectionId, category.CategoryId, operationToken) ?? -1;
                            category.SectionOrder = maxSectionOrder + 1;
                        }

                        if (category.GlobalOrder is null ||
                            await categories.HasActiveGlobalRootWithOrderAsync(category.GlobalOrder.Value, category.CategoryId, operationToken))
                        {
                            var maxGlobalOrder = await categories.GetMaxGlobalOrderAsync(category.CategoryId, operationToken) ?? -1;
                            category.GlobalOrder = maxGlobalOrder + 1;
                        }
                    }

                    category.IsDeleted = false;
                    category.DeletedAtUtc = null;
                    category.DeletionOperationId = null;
                }

                revision.TreeRevision += 1;

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("category.subtree_restored", new
                    {
                        command.DeletionOperationId,
                        restoredCategoryIds = restoredIds,
                    })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    private static AbwabWriteConflictException Unavailable() =>
        new(AbwabConflictCodes.CategoryUnavailable, "Category is unavailable.");
}
