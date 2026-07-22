using System.Reflection;
using QuranDashboard.Application.Security.Owners;
using QuranDashboard.Application.Security.Permissions;

namespace QuranDashboard.Application.Abwab.Concurrency;

// Every Abwab writer MUST be registered here; an unregistered writer bypasses the barrier gate (startup fail-fast).
public static class AbwabWriterRegistrations
{
    public static IReadOnlyList<Assembly> WriterAssemblies { get; } = [typeof(AbwabWriterRegistrations).Assembly];

    public static void RegisterAll(AbwabWriterRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register<GrantPermissionCommand>();
        registry.Register<RevokePermissionCommand>();
        registry.Register<AddSystemOwnerCommand>();
        registry.Register<RemoveSystemOwnerCommand>();
        registry.Register<BootstrapSystemOwnerCommand>();
    }
}
