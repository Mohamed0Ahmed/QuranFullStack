using System.Reflection;
using QuranDashboard.Application.Security.Owners;
using QuranDashboard.Application.Security.Permissions;

namespace QuranDashboard.Application.Abwab.Concurrency;

// The canonical, EXPLICIT list of Abwab writers wired through the barrier gate, plus the assemblies the
// stabilization guard scans for IAbwabWriter implementations. 028 has zero domain writers. Every future
// 029+ writer MUST be added to RegisterAll; the stabilization registry test (and the startup fail-fast)
// fails if a discovered IAbwabWriter is absent here — that is exactly a writer bypassing the barrier.
public static class AbwabWriterRegistrations
{
    public static IReadOnlyList<Assembly> WriterAssemblies { get; } = [typeof(AbwabWriterRegistrations).Assembly];

    public static void RegisterAll(AbwabWriterRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        // US5 security writers. These are IAbwabMutationCommand writers: they route through the separate
        // security-audit unit of work, which still takes the AbwabWriteBarrier + AbwabRevisionState locks
        // and carries ExpectedTimelineGeneration (it just never advances the product head). Every one MUST
        // be registered here or the startup fail-fast (and the registry test) rejects it as a bypass.
        registry.Register<GrantPermissionCommand>();
        registry.Register<RevokePermissionCommand>();
        registry.Register<AddSystemOwnerCommand>();
        registry.Register<RemoveSystemOwnerCommand>();
        registry.Register<BootstrapSystemOwnerCommand>();

        // Future 029+ domain writers are registered here as well.
    }
}
