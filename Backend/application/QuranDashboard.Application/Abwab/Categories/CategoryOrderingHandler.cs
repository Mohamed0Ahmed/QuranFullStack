using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abwab.Categories;

public sealed class CategoryOrderingHandler(
    IAbwabWriteExecutor executor,
    ICategoryTreeStore categories,
    ISectionWriteStore sections,
    ProtectionResolver protectionResolver,
    ISystemOwnerStore systemOwners,
    IServerClock clock)
{
    public async Task MoveAsync(MoveCategoriesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Moves.Count == 0)
        {
            throw new ArgumentException("At least one move entry is required.", nameof(command));
        }

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (revision, operationToken) =>
            {
                AbwabRevisionGuards.GuardTreeRevision(revision, command.ExpectedTreeRevision);

                var selectedIds = command.Moves.Select(m => m.CategoryId).ToList();
                if (selectedIds.Distinct().Count() != selectedIds.Count)
                {
                    throw Conflict(AbwabConflictCodes.CategoryOverlappingMove, "Duplicate categories in the same move request.");
                }

                var selected = await categories.FindManyTrackedAsync(selectedIds, operationToken);
                if (selected.Count != selectedIds.Count)
                {
                    throw Unavailable();
                }

                var selectedSet = selectedIds.ToHashSet();
                foreach (var category in selected)
                {
                    if (category.AncestorIds.Any(selectedSet.Contains))
                    {
                        throw Conflict(AbwabConflictCodes.CategoryOverlappingMove, "A selected category is a descendant of another selected category.");
                    }
                }

                var events = new List<AbwabAuditEventDraft>();
                var eventOrdinal = 0;
                var now = clock.UtcNow;

                foreach (var move in command.Moves)
                {
                    var category = selected.Single(c => c.CategoryId == move.CategoryId);
                    AbwabRevisionGuards.GuardRowVersion(category.Version, move.ExpectedVersion);

                    if (move.NewParentCategoryId == category.CategoryId)
                    {
                        throw Conflict(AbwabConflictCodes.CategoryCycle, "A category cannot become its own parent.");
                    }

                    if (move.NewParentCategoryId is { } destinationId && selectedSet.Contains(destinationId))
                    {
                        throw Conflict(AbwabConflictCodes.CategoryOverlappingMove, "Destination is inside the same batch of selected categories.");
                    }

                    // §9 Move row: "Each selected category CategoryData; old/new parent InternalStructure;
                    // inherited scopes included." The moved category's own child-set does not change on a
                    // move, so it is gated on CategoryData (its placement/data), not InternalStructure — and
                    // it is the only target that starts/restarts the ordinary window.
                    await CategoryProtectionGate.EnsureOrdinaryEditAllowedAsync(
                        protectionResolver, systemOwners, category.CategoryId, ManualProtectionType.CategoryData, command.ActorSubject, operationToken);

                    var oldParentId = category.ParentCategoryId;
                    var oldSectionId = category.SectionId;
                    var oldGlobalOrder = category.GlobalOrder;
                    var wasRoot = category.ParentCategoryId is null;

                    // The old parent's child-set loses this category — its InternalStructure protection
                    // (manual only, no ordinary window: the parent is not the selected target) must block too.
                    if (oldParentId is { } existingParentId)
                    {
                        await CategoryProtectionGate.EnsureNotManuallyProtectedAsync(
                            protectionResolver, existingParentId, ManualProtectionType.InternalStructure, operationToken);
                    }

                    List<Guid> newAncestorIds;

                    if (move.NewParentCategoryId is { } newParentId)
                    {
                        var newParent = await categories.FindTrackedAsync(newParentId, includeDeleted: false, operationToken)
                            ?? throw Unavailable();

                        if (newParent.AncestorIds.Contains(category.CategoryId))
                        {
                            throw Conflict(AbwabConflictCodes.CategoryCycle, "Destination is inside the moved subtree.");
                        }

                        // The new parent's child-set gains this category — its InternalStructure protection
                        // must block too, closing the "move a node into a protected parent" bypass.
                        await CategoryProtectionGate.EnsureNotManuallyProtectedAsync(
                            protectionResolver, newParentId, ManualProtectionType.InternalStructure, operationToken);

                        await CategoryTreeGuards.GuardNameConflictAsync(categories, category.NormalizedName, newParentId, category.CategoryId, operationToken);

                        var maxSibling = await categories.GetMaxSiblingOrderAsync(newParentId, category.CategoryId, operationToken) ?? -1;

                        newAncestorIds = [.. newParent.AncestorIds, newParent.CategoryId];

                        category.ParentCategoryId = newParentId;
                        category.SectionId = null;
                        category.SiblingOrder = maxSibling + 1;
                        category.SectionOrder = null;
                        category.GlobalOrder = null;
                    }
                    else
                    {
                        var destinationSectionId = move.NewSectionId
                            ?? (wasRoot ? oldSectionId!.Value : await sections.GetPermanentDefaultSectionIdAsync(operationToken));

                        if (!await sections.ExistsActiveAsync(destinationSectionId, operationToken))
                        {
                            throw Unavailable();
                        }

                        if (!wasRoot)
                        {
                            await CategoryTreeGuards.GuardNameConflictAsync(categories, category.NormalizedName, null, category.CategoryId, operationToken);
                        }

                        var maxSectionOrder = await categories.GetMaxSectionOrderAsync(destinationSectionId, category.CategoryId, operationToken) ?? -1;

                        newAncestorIds = [];

                        category.ParentCategoryId = null;
                        category.SectionId = destinationSectionId;
                        category.SiblingOrder = null;
                        category.SectionOrder = maxSectionOrder + 1;

                        if (wasRoot)
                        {
                            category.GlobalOrder = oldGlobalOrder;
                        }
                        else
                        {
                            var maxGlobalOrder = await categories.GetMaxGlobalOrderAsync(category.CategoryId, operationToken) ?? -1;
                            category.GlobalOrder = maxGlobalOrder + 1;
                        }
                    }

                    category.AncestorIds = newAncestorIds;
                    category.Depth = newAncestorIds.Count;

                    await RewriteDescendantAncestryAsync(category.CategoryId, newAncestorIds, operationToken);

                    CategoryProtectionGate.StartWindow(category, command.ActorSubject, now);

                    events.Add(new AbwabAuditEventDraft(eventOrdinal++, AbwabAuditPayload.Serialize("category.moved", new
                    {
                        category.CategoryId,
                        oldParentId,
                        newParentId = category.ParentCategoryId,
                        oldSectionId,
                        newSectionId = category.SectionId,
                    })));
                }

                revision.TreeRevision += 1;
                return AbwabAuditedOperationOutcome.Audited(events);
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    public async Task ReorderAsync(ReorderCategoriesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Orders.Count == 0)
        {
            throw new ArgumentException("At least one order entry is required.", nameof(command));
        }

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (revision, operationToken) =>
            {
                AbwabRevisionGuards.GuardTreeRevision(revision, command.ExpectedTreeRevision);

                // §9 "Reorder child SiblingOrder": parent InternalStructure, on top of each reordered
                // category's own CategoryData below. §9 "Reorder root SectionOrder/GlobalOrder" has no
                // parent to gate — roots are not a category's children.
                if (command.Scope == CategoryOrderScope.Siblings && command.ParentCategoryId is { } parentId)
                {
                    await CategoryProtectionGate.EnsureNotManuallyProtectedAsync(
                        protectionResolver, parentId, ManualProtectionType.InternalStructure, operationToken);
                }

                var members = command.Scope switch
                {
                    CategoryOrderScope.Siblings => await categories.FindActiveSiblingsAsync(command.ParentCategoryId!.Value, operationToken),
                    CategoryOrderScope.SectionRoots => await categories.FindActiveSectionRootsAsync(command.SectionId!.Value, operationToken),
                    CategoryOrderScope.GlobalRoots => await categories.FindActiveGlobalRootsAsync(operationToken),
                    _ => throw new ArgumentOutOfRangeException(nameof(command)),
                };

                var ids = command.Orders.Select(o => o.CategoryId).ToHashSet();

                if (members.Count != ids.Count || members.Any(m => !ids.Contains(m.CategoryId)))
                {
                    throw Conflict(AbwabConflictCodes.CategoryUnavailable, "The reorder set does not match the current scope membership.");
                }

                var byId = members.ToDictionary(m => m.CategoryId);
                foreach (var entry in command.Orders)
                {
                    AbwabRevisionGuards.GuardRowVersion(byId[entry.CategoryId].Version, entry.ExpectedVersion);
                }

                // §9 "Reordered category/root CategoryData" — every reordered category, both scopes, manual
                // only (reorder is never ordinary-gated, per the "No" ordinary column).
                foreach (var entry in command.Orders)
                {
                    await CategoryProtectionGate.EnsureNotManuallyProtectedAsync(
                        protectionResolver, entry.CategoryId, ManualProtectionType.CategoryData, operationToken);
                }

                foreach (var entry in command.Orders)
                {
                    var category = byId[entry.CategoryId];
                    switch (command.Scope)
                    {
                        case CategoryOrderScope.Siblings:
                            category.SiblingOrder = entry.NewOrder;
                            break;
                        case CategoryOrderScope.SectionRoots:
                            category.SectionOrder = entry.NewOrder;
                            break;
                        case CategoryOrderScope.GlobalRoots:
                            category.GlobalOrder = entry.NewOrder;
                            break;
                    }
                }

                revision.TreeRevision += 1;

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("category.reordered", new { command.Scope, command.ParentCategoryId, command.SectionId, command.Orders })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    private async Task RewriteDescendantAncestryAsync(Guid movedCategoryId, IReadOnlyList<Guid> newMovedAncestorIds, CancellationToken cancellationToken)
    {
        var descendants = await categories.FindDescendantsAsync(movedCategoryId, cancellationToken);

        foreach (var descendant in descendants)
        {
            var movedIndex = descendant.AncestorIds.IndexOf(movedCategoryId);
            var suffix = descendant.AncestorIds.Skip(movedIndex + 1).ToList();
            var rewritten = new List<Guid>(newMovedAncestorIds) { movedCategoryId };
            rewritten.AddRange(suffix);

            descendant.AncestorIds = rewritten;
            descendant.Depth = rewritten.Count;
        }
    }

    private static AbwabWriteConflictException Unavailable() =>
        new(AbwabConflictCodes.CategoryUnavailable, "Category is unavailable.");

    private static AbwabWriteConflictException Conflict(string code, string message) => new(code, message);
}
