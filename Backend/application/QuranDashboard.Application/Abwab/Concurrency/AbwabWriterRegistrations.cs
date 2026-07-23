using System.Reflection;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Security.Owners;
using QuranDashboard.Application.Security.Permissions;

namespace QuranDashboard.Application.Abwab.Concurrency;

// Every Abwab writer MUST be registered here; an unregistered writer bypasses the barrier gate (startup fail-fast).
public static class AbwabWriterRegistrations
{
    public static IReadOnlyList<Assembly> WriterAssemblies { get; } =
        [typeof(AbwabWriterRegistrations).Assembly, typeof(AddSectionCommand).Assembly];

    public static void RegisterAll(AbwabWriterRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register<GrantPermissionCommand>();
        registry.Register<RevokePermissionCommand>();
        registry.Register<AddSystemOwnerCommand>();
        registry.Register<RemoveSystemOwnerCommand>();
        registry.Register<BootstrapSystemOwnerCommand>();

        registry.Register<AddSectionCommand>();
        registry.Register<EditSectionCommand>();
        registry.Register<ReorderSectionsCommand>();
        registry.Register<DeleteSectionCommand>();

        registry.Register<AddCategoryCommand>();
        registry.Register<EditCategoryCommand>();
        registry.Register<MoveCategoriesCommand>();
        registry.Register<ReorderCategoriesCommand>();
        registry.Register<SubtreeDeleteCommand>();
        registry.Register<OperationRestoreCommand>();
        registry.Register<AddCategoryAliasCommand>();
        registry.Register<EditCategoryAliasCommand>();
        registry.Register<RemoveCategoryAliasCommand>();

        registry.Register<ApplyManualProtectionCommand>();
        registry.Register<LiftManualProtectionCommand>();
        registry.Register<ApplyFullProtectionPresetCommand>();
    }
}
