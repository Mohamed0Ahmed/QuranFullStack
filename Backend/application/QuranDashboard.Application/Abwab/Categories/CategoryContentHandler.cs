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
        Category? category = null;

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (revision, operationToken) =>
            {
                AbwabRevisionGuards.GuardTreeRevision(revision, command.ExpectedTreeRevision);

                var creation = new CategoryGroupedCreation(categories, sections, protectionResolver);
                var input = new CategoryCreationInput(command.Name, command.RepresentativeQuranExcerpt, command.Description);

                category = command.ParentCategoryId is { } parentId
                    ? await creation.AddChildAsync(parentId, input, operationToken)
                    : await creation.AddRootAsync(command.SectionId, input, operationToken);

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
        return category?.CategoryId ?? throw new InvalidOperationException("The audited operation did not create a category.");
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
