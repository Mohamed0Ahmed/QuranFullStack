using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Application.Abwab.Categories;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Templates;

namespace QuranDashboard.Application.Abwab.Templates;

// Applies ONE template to ONE target category. Real categories are written ONLY through the accepted
// 029 creation core, reached via CategoryGroupedCreation — this feature adds no second category
// writer. The whole application is one audited operation: one ChangeSet, one TreeRevision bump.
public sealed class TemplateApplicationHandler(
    IAbwabWriteExecutor executor,
    IDoorTemplateStore templates,
    ITemplateNodeStore nodes,
    ITemplateNodeAliasStore templateAliases,
    ICategoryTreeStore categories,
    ISectionWriteStore sections,
    ICategorySearchAliasWriteStore categoryAliases,
    ProtectionResolver protectionResolver)
{
    public async Task ApplyAsync(ApplyDoorTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (revision, operationToken) =>
            {
                AbwabRevisionGuards.GuardTreeRevision(revision, command.ExpectedTreeRevision);

                var target = await categories.FindTrackedAsync(command.TargetCategoryId, includeDeleted: false, operationToken)
                    ?? throw Unavailable();
                AbwabRevisionGuards.GuardRowVersion(target.Version, command.ExpectedTargetVersion);

                var template = await templates.FindTrackedAsync(command.DoorTemplateId, operationToken) ?? throw TemplateTreeGuards.Stale();
                if (template.IsDeleted)
                {
                    throw TemplateTreeGuards.Stale();
                }

                TemplateTreeGuards.GuardTemplateRevision(template, command.ExpectedTemplateRevision);

                // Hoisted out of the recursion deliberately: a template with zero active nodes creates
                // no child, so a gate reached only per created root would let an empty template commit
                // a TreeRevision bump and an audit event against a protected target.
                await CategoryProtectionGate.EnsureNotManuallyProtectedAsync(
                    protectionResolver, target.CategoryId, ManualProtectionType.InternalStructure, operationToken);

                var allNodes = await nodes.FindTrackedForTemplateAsync(template.DoorTemplateId, operationToken);
                TemplateTreeGuards.GuardAcyclic(allNodes);

                var activeNodes = allNodes.Where(node => !node.IsDeleted).ToList();
                var allAliases = await templateAliases.FindTrackedForNodesAsync(
                    [.. activeNodes.Select(node => node.TemplateNodeId)], operationToken);

                var creation = new CategoryGroupedCreation(categories, sections, protectionResolver);
                var createdTree = await CreateChildrenAsync(creation, activeNodes, allAliases, target.CategoryId, null, level: 1, operationToken);

                revision.TreeRevision += 1;

                var payload = new TemplateApplicationAuditView(
                    template.DoorTemplateId,
                    template.Name,
                    TemplateAuditProjections.Snapshot(template, allNodes, allAliases),
                    target.CategoryId,
                    await TargetPathAsync(target, operationToken),
                    createdTree,
                    CountsByLevel(createdTree));

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize(TemplateAuditActions.Applied, payload)));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    // Every template ROOT becomes a direct child of the target, and the recursion copies exactly the
    // §7.4 allowlist — name, representative excerpt, description, aliases, order, structure.
    private async Task<IReadOnlyList<CreatedCategoryAuditView>> CreateChildrenAsync(
        CategoryGroupedCreation creation,
        IReadOnlyList<TemplateNode> activeNodes,
        IReadOnlyList<TemplateNodeSearchAlias> allAliases,
        Guid parentCategoryId,
        Guid? parentTemplateNodeId,
        int level,
        CancellationToken cancellationToken)
    {
        var siblings = activeNodes
            .Where(node => node.ParentTemplateNodeId == parentTemplateNodeId)
            .OrderBy(node => node.SiblingOrder)
            .ToList();

        var created = new List<CreatedCategoryAuditView>();
        foreach (var node in siblings)
        {
            var category = await creation.AddChildAsync(
                parentCategoryId,
                new CategoryCreationInput(node.Name, node.RepresentativeQuranExcerpt, node.Description),
                cancellationToken);

            var copiedAliases = CopyAliases(allAliases, node.TemplateNodeId, category.CategoryId);

            var children = await CreateChildrenAsync(
                creation, activeNodes, allAliases, category.CategoryId, node.TemplateNodeId, level + 1, cancellationToken);

            created.Add(new CreatedCategoryAuditView(
                category.CategoryId,
                category.Name,
                category.RepresentativeQuranExcerpt,
                category.Description,
                level,
                category.SiblingOrder ?? 0,
                copiedAliases,
                children));
        }

        return created;
    }

    // Deduped by normalized value: a template node may legally hold two aliases that normalize to the
    // same string (§7.4 defines no template-alias uniqueness rule), but the category alias index IS
    // unique — copying both would fail the constraint as an unmapped 500 instead of a §11 conflict.
    private IReadOnlyList<string> CopyAliases(
        IReadOnlyList<TemplateNodeSearchAlias> allAliases,
        Guid templateNodeId,
        Guid categoryId)
    {
        var values = new List<string>();

        foreach (var alias in allAliases
            .Where(a => a.TemplateNodeId == templateNodeId && !a.IsDeleted)
            .DistinctBy(a => a.NormalizedValue, StringComparer.Ordinal))
        {
            categoryAliases.Add(new CategorySearchAlias
            {
                CategorySearchAliasId = Guid.NewGuid(),
                CategoryId = categoryId,
                Value = alias.Value,
                NormalizedValue = alias.NormalizedValue,
            });

            values.Add(alias.Value);
        }

        return values;
    }

    private async Task<IReadOnlyList<string>> TargetPathAsync(Category target, CancellationToken cancellationToken)
    {
        if (target.AncestorIds.Count == 0)
        {
            return [target.Name];
        }

        var ancestors = await categories.FindManyTrackedAsync(target.AncestorIds, cancellationToken);
        var namesById = ancestors.ToDictionary(ancestor => ancestor.CategoryId, ancestor => ancestor.Name);

        // The store filters soft-deleted rows, so an active target under a soft-deleted ancestor
        // yields a chain this operation cannot address — a mapped conflict, not a lookup crash.
        if (target.AncestorIds.Any(id => !namesById.ContainsKey(id)))
        {
            throw Unavailable();
        }

        return [.. target.AncestorIds.Select(id => namesById[id]), target.Name];
    }

    private static AbwabWriteConflictException Unavailable() =>
        new(AbwabConflictCodes.CategoryUnavailable, "Category is unavailable.");

    private static IReadOnlyList<TemplateLevelCountAuditView> CountsByLevel(IReadOnlyList<CreatedCategoryAuditView> createdTree)
    {
        var counts = new SortedDictionary<int, int>();
        Accumulate(createdTree);
        return [.. counts.Select(entry => new TemplateLevelCountAuditView(entry.Key, entry.Value))];

        void Accumulate(IReadOnlyList<CreatedCategoryAuditView> level)
        {
            foreach (var entry in level)
            {
                counts[entry.Level] = counts.GetValueOrDefault(entry.Level) + 1;
                Accumulate(entry.Children);
            }
        }
    }
}
