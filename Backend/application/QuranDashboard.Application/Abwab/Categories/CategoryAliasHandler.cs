using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Tree;

namespace QuranDashboard.Application.Abwab.Categories;

// T056: alias add/edit/remove is category direct-content mutation under category.edit — never a
// borrowed child add/delete verb; removal is tracked soft delete; physical delete is rejected by
// the 028 SavingChanges guard. Ordinary-gated and bumps CategoryContentRevision once (T059/T081).
public sealed class CategoryAliasHandler(
    IAbwabWriteExecutor executor,
    ICategoryTreeStore categories,
    ICategorySearchAliasWriteStore aliases,
    ProtectionResolver protectionResolver,
    ISystemOwnerStore systemOwners,
    IServerClock clock)
{
    public async Task<Guid> AddAsync(AddCategoryAliasCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var aliasId = Guid.NewGuid();

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (_, operationToken) =>
            {
                var category = await RequireCategoryAsync(command.CategoryId, operationToken);

                await CategoryProtectionGate.EnsureOrdinaryEditAllowedAsync(
                    protectionResolver, systemOwners, category.CategoryId, ManualProtectionType.CategoryData, command.ActorSubject, operationToken);

                var normalized = ArabicNameNormalizer.Normalize(command.Value);
                await GuardAliasConflictAsync(category.CategoryId, normalized, null, operationToken);

                aliases.Add(new CategorySearchAlias
                {
                    CategorySearchAliasId = aliasId,
                    CategoryId = category.CategoryId,
                    Value = command.Value,
                    NormalizedValue = normalized,
                });

                category.CategoryContentRevision += 1;
                CategoryProtectionGate.StartWindow(category, command.ActorSubject, clock.UtcNow);

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("category.alias_added", new { category.CategoryId, aliasId, command.Value })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
        return aliasId;
    }

    public async Task EditAsync(EditCategoryAliasCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (_, operationToken) =>
            {
                var alias = await RequireAliasAsync(command.CategorySearchAliasId, operationToken);
                AbwabRevisionGuards.GuardRowVersion(alias.Version, command.ExpectedVersion);

                var category = await RequireCategoryAsync(alias.CategoryId, operationToken);
                await CategoryProtectionGate.EnsureOrdinaryEditAllowedAsync(
                    protectionResolver, systemOwners, category.CategoryId, ManualProtectionType.CategoryData, command.ActorSubject, operationToken);

                var normalized = ArabicNameNormalizer.Normalize(command.Value);
                await GuardAliasConflictAsync(alias.CategoryId, normalized, alias.CategorySearchAliasId, operationToken);

                var oldValue = alias.Value;
                alias.Value = command.Value;
                alias.NormalizedValue = normalized;

                category.CategoryContentRevision += 1;
                CategoryProtectionGate.StartWindow(category, command.ActorSubject, clock.UtcNow);

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("category.alias_edited", new { alias.CategorySearchAliasId, oldValue, newValue = alias.Value })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    public async Task RemoveAsync(RemoveCategoryAliasCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (_, operationToken) =>
            {
                var alias = await RequireAliasAsync(command.CategorySearchAliasId, operationToken);
                AbwabRevisionGuards.GuardRowVersion(alias.Version, command.ExpectedVersion);

                var category = await RequireCategoryAsync(alias.CategoryId, operationToken);
                await CategoryProtectionGate.EnsureOrdinaryEditAllowedAsync(
                    protectionResolver, systemOwners, category.CategoryId, ManualProtectionType.CategoryData, command.ActorSubject, operationToken);

                alias.IsDeleted = true;
                alias.DeletedAtUtc = clock.UtcNow;

                category.CategoryContentRevision += 1;
                CategoryProtectionGate.StartWindow(category, command.ActorSubject, clock.UtcNow);

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("category.alias_removed", new { alias.CategorySearchAliasId, alias.CategoryId })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    private async Task<Category> RequireCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        await categories.FindTrackedAsync(categoryId, includeDeleted: false, cancellationToken)
            ?? throw new AbwabWriteConflictException(AbwabConflictCodes.CategoryUnavailable, "Category is unavailable.");

    private async Task<CategorySearchAlias> RequireAliasAsync(Guid aliasId, CancellationToken cancellationToken) =>
        await aliases.FindTrackedAsync(aliasId, cancellationToken)
            ?? throw new AbwabWriteConflictException(AbwabConflictCodes.CategoryUnavailable, "Alias is unavailable.");

    private async Task GuardAliasConflictAsync(Guid categoryId, string normalizedValue, Guid? excludeAliasId, CancellationToken cancellationToken)
    {
        if (await aliases.ActiveNormalizedValueExistsAsync(categoryId, normalizedValue, excludeAliasId, cancellationToken))
        {
            throw new AbwabWriteConflictException(AbwabConflictCodes.CategoryAliasConflict, "A duplicate active alias already exists on this category.");
        }
    }
}
