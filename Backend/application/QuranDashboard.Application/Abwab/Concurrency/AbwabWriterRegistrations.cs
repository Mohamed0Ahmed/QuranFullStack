using System.Reflection;

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

        // No Abwab domain writers exist in 028. Register each 029+ writer here, e.g.:
        //   registry.Register<CreateSectionCommandHandler>();
    }
}
