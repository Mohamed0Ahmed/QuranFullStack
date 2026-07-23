namespace QuranDashboard.Application.Abstractions.Abwab.Core;

// Every command carries ExpectedTimelineGeneration (via IAbwabMutationCommand) and, where structural,
// expected xmin/TreeRevision. Explicit action methods only — no drag semantics. Consumed directly by the
// API layer today; the 029 US4 frontend core mock implements the same command/DTO contract for parity.
public interface IAbwabCoreWritePort
{
    Task<Guid> AddSectionAsync(AddSectionCommand command, CancellationToken cancellationToken);

    Task EditSectionAsync(EditSectionCommand command, CancellationToken cancellationToken);

    Task ReorderSectionsAsync(ReorderSectionsCommand command, CancellationToken cancellationToken);

    Task DeleteSectionAsync(DeleteSectionCommand command, CancellationToken cancellationToken);

    Task<Guid> AddCategoryAsync(AddCategoryCommand command, CancellationToken cancellationToken);

    Task EditCategoryAsync(EditCategoryCommand command, CancellationToken cancellationToken);

    Task MoveCategoriesAsync(MoveCategoriesCommand command, CancellationToken cancellationToken);

    Task ReorderCategoriesAsync(ReorderCategoriesCommand command, CancellationToken cancellationToken);

    Task<Guid> SubtreeDeleteAsync(SubtreeDeleteCommand command, CancellationToken cancellationToken);

    Task OperationRestoreAsync(OperationRestoreCommand command, CancellationToken cancellationToken);

    Task<Guid> AddCategoryAliasAsync(AddCategoryAliasCommand command, CancellationToken cancellationToken);

    Task EditCategoryAliasAsync(EditCategoryAliasCommand command, CancellationToken cancellationToken);

    Task RemoveCategoryAliasAsync(RemoveCategoryAliasCommand command, CancellationToken cancellationToken);

    Task ApplyManualProtectionAsync(ApplyManualProtectionCommand command, CancellationToken cancellationToken);

    Task LiftManualProtectionAsync(LiftManualProtectionCommand command, CancellationToken cancellationToken);

    Task ApplyFullProtectionPresetAsync(ApplyFullProtectionPresetCommand command, CancellationToken cancellationToken);
}
