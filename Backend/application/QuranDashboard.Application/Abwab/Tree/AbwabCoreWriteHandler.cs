using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abwab.Categories;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Application.Abwab.Sections;

namespace QuranDashboard.Application.Abwab.Tree;

// T050: the single Application-owned implementation of the core write port, composing the per-area
// handlers (T052-T058). No business logic lives here — it is a thin, testable seam the API layer and
// (in US4) the frontend core mock both target.
public sealed class AbwabCoreWriteHandler(
    SectionWriterHandler sections,
    CategoryContentHandler categoryContent,
    CategoryOrderingHandler categoryOrdering,
    CategorySubtreeHandler categorySubtree,
    CategoryAliasHandler categoryAlias,
    ManualProtectionWriterHandler manualProtection,
    FullProtectionPresetHandler fullProtectionPreset) : IAbwabCoreWritePort
{
    public Task<Guid> AddSectionAsync(AddSectionCommand command, CancellationToken cancellationToken) =>
        sections.AddAsync(command, cancellationToken);

    public Task EditSectionAsync(EditSectionCommand command, CancellationToken cancellationToken) =>
        sections.EditAsync(command, cancellationToken);

    public Task ReorderSectionsAsync(ReorderSectionsCommand command, CancellationToken cancellationToken) =>
        sections.ReorderAsync(command, cancellationToken);

    public Task DeleteSectionAsync(DeleteSectionCommand command, CancellationToken cancellationToken) =>
        sections.DeleteAsync(command, cancellationToken);

    public Task<Guid> AddCategoryAsync(AddCategoryCommand command, CancellationToken cancellationToken) =>
        categoryContent.AddAsync(command, cancellationToken);

    public Task EditCategoryAsync(EditCategoryCommand command, CancellationToken cancellationToken) =>
        categoryContent.EditAsync(command, cancellationToken);

    public Task MoveCategoriesAsync(MoveCategoriesCommand command, CancellationToken cancellationToken) =>
        categoryOrdering.MoveAsync(command, cancellationToken);

    public Task ReorderCategoriesAsync(ReorderCategoriesCommand command, CancellationToken cancellationToken) =>
        categoryOrdering.ReorderAsync(command, cancellationToken);

    public Task<Guid> SubtreeDeleteAsync(SubtreeDeleteCommand command, CancellationToken cancellationToken) =>
        categorySubtree.DeleteAsync(command, cancellationToken);

    public Task OperationRestoreAsync(OperationRestoreCommand command, CancellationToken cancellationToken) =>
        categorySubtree.RestoreAsync(command, cancellationToken);

    public Task<Guid> AddCategoryAliasAsync(AddCategoryAliasCommand command, CancellationToken cancellationToken) =>
        categoryAlias.AddAsync(command, cancellationToken);

    public Task EditCategoryAliasAsync(EditCategoryAliasCommand command, CancellationToken cancellationToken) =>
        categoryAlias.EditAsync(command, cancellationToken);

    public Task RemoveCategoryAliasAsync(RemoveCategoryAliasCommand command, CancellationToken cancellationToken) =>
        categoryAlias.RemoveAsync(command, cancellationToken);

    public Task ApplyManualProtectionAsync(ApplyManualProtectionCommand command, CancellationToken cancellationToken) =>
        manualProtection.ApplyAsync(command, cancellationToken);

    public Task LiftManualProtectionAsync(LiftManualProtectionCommand command, CancellationToken cancellationToken) =>
        manualProtection.LiftAsync(command, cancellationToken);

    public Task ApplyFullProtectionPresetAsync(ApplyFullProtectionPresetCommand command, CancellationToken cancellationToken) =>
        fullProtectionPreset.ApplyAsync(command, cancellationToken);
}
