using System.Reflection;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
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

        registry.Register<AddRelationshipCommand>();
        registry.Register<EditRelationshipCommand>();
        registry.Register<DeleteRelationshipCommand>();
        registry.Register<RestoreRelationshipCommand>();

        registry.Register<AddDoorTemplateCommand>();
        registry.Register<EditDoorTemplateCommand>();
        registry.Register<DeleteDoorTemplateCommand>();
        registry.Register<RestoreDoorTemplateCommand>();
        registry.Register<AddTemplateNodeCommand>();
        registry.Register<EditTemplateNodeCommand>();
        registry.Register<ReparentTemplateNodeCommand>();
        registry.Register<ReorderTemplateNodesCommand>();
        registry.Register<RemoveTemplateNodeCommand>();
        registry.Register<AddTemplateNodeAliasCommand>();
        registry.Register<EditTemplateNodeAliasCommand>();
        registry.Register<RemoveTemplateNodeAliasCommand>();
        registry.Register<RestoreTemplateNodeAliasCommand>();
        registry.Register<ApplyDoorTemplateCommand>();
    }
}
