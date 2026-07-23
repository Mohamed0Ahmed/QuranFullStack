using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Tree;

namespace QuranDashboard.Application.Abwab.Categories;

public sealed class CategoryContentHandler(
    IAbwabWriteExecutor executor,
    ICategoryTreeStore categories,
    ISectionWriteStore sections,
    ProtectionResolver protectionResolver,
    ISystemOwnerStore systemOwners,
    IServerClock clock)
{
    public async Task<Guid> AddAsync(AddCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var categoryId = Guid.NewGuid();

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (revision, operationToken) =>
            {
                AbwabRevisionGuards.GuardTreeRevision(revision, command.ExpectedTreeRevision);
                var normalized = ArabicNameNormalizer.Normalize(command.Name);

                Category category;
                if (command.ParentCategoryId is { } parentId)
                {
                    var parent = await categories.FindTrackedAsync(parentId, includeDeleted: false, operationToken)
                        ?? throw Unavailable();

                    await CategoryProtectionGate.EnsureNotManuallyProtectedAsync(
                        protectionResolver, parentId, ManualProtectionType.InternalStructure, operationToken);

                    await CategoryTreeGuards.GuardNameConflictAsync(categories, normalized, parentId, null, operationToken);

                    var maxSibling = await categories.GetMaxSiblingOrderAsync(parentId, null, operationToken) ?? -1;

                    category = new Category
                    {
                        CategoryId = categoryId,
                        Name = command.Name,
                        NormalizedName = normalized,
                        Description = command.Description,
                        RepresentativeQuranExcerpt = command.RepresentativeQuranExcerpt,
                        ParentCategoryId = parentId,
                        SectionId = null,
                        SiblingOrder = maxSibling + 1,
                        SectionOrder = null,
                        GlobalOrder = null,
                        AncestorIds = [.. parent.AncestorIds, parent.CategoryId],
                        Depth = parent.AncestorIds.Count + 1,
                    };
                }
                else
                {
                    var sectionId = command.SectionId ?? await sections.GetPermanentDefaultSectionIdAsync(operationToken);

                    if (!await sections.ExistsActiveAsync(sectionId, operationToken))
                    {
                        throw Unavailable();
                    }

                    await CategoryTreeGuards.GuardNameConflictAsync(categories, normalized, null, null, operationToken);

                    var maxSectionOrder = await categories.GetMaxSectionOrderAsync(sectionId, null, operationToken) ?? -1;
                    var maxGlobalOrder = await categories.GetMaxGlobalOrderAsync(null, operationToken) ?? -1;

                    category = new Category
                    {
                        CategoryId = categoryId,
                        Name = command.Name,
                        NormalizedName = normalized,
                        Description = command.Description,
                        RepresentativeQuranExcerpt = command.RepresentativeQuranExcerpt,
                        ParentCategoryId = null,
                        SectionId = sectionId,
                        SiblingOrder = null,
                        SectionOrder = maxSectionOrder + 1,
                        GlobalOrder = maxGlobalOrder + 1,
                        AncestorIds = [],
                        Depth = 0,
                    };
                }

                categories.Add(category);
                revision.TreeRevision += 1;

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("category.added", new
                    {
                        category.CategoryId,
                        category.Name,
                        category.ParentCategoryId,
                        category.SectionId,
                    })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
        return categoryId;
    }

    public async Task EditAsync(EditCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (_, operationToken) =>
            {
                var category = await categories.FindTrackedAsync(command.CategoryId, includeDeleted: false, operationToken)
                    ?? throw Unavailable();

                AbwabRevisionGuards.GuardRowVersion(category.Version, command.ExpectedVersion);

                await CategoryProtectionGate.EnsureOrdinaryEditAllowedAsync(
                    protectionResolver, systemOwners, category.CategoryId, ManualProtectionType.CategoryData, command.ActorSubject, operationToken);

                var normalized = ArabicNameNormalizer.Normalize(command.Name);
                if (!string.Equals(normalized, category.NormalizedName, StringComparison.Ordinal))
                {
                    await CategoryTreeGuards.GuardNameConflictAsync(categories, normalized, category.ParentCategoryId, category.CategoryId, operationToken);
                }

                var oldName = category.Name;
                category.Name = command.Name;
                category.NormalizedName = normalized;
                category.Description = command.Description;
                category.RepresentativeQuranExcerpt = command.RepresentativeQuranExcerpt;
                category.CategoryContentRevision += 1;

                CategoryProtectionGate.StartWindow(category, command.ActorSubject, clock.UtcNow);

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("category.edited", new
                    {
                        category.CategoryId,
                        oldName,
                        newName = category.Name,
                        category.Description,
                        category.RepresentativeQuranExcerpt,
                    })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    private static AbwabWriteConflictException Unavailable() =>
        new(AbwabConflictCodes.CategoryUnavailable, "Category is unavailable.");
}
