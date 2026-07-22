using System.Reflection;
using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Concurrency;

// The mechanism behind the stabilization registry test: discover every concrete IAbwabWriter and report
// any that is not registered against the barrier. Fail-closed — an unregistered writer is a bypass of the
// global AbwabWriteBarrier gate and must break the build.
public static class AbwabWriterStabilizationGuard
{
    public static IReadOnlyList<Type> DiscoverWriters(IEnumerable<Assembly> assemblies) =>
        assemblies
            .SelectMany(GetLoadableTypes)
            .Where(IsConcreteWriter)
            .Distinct()
            .ToList();

    public static IReadOnlyList<Type> FindWritersMissingBarrier(
        IEnumerable<Type> discoveredWriters,
        IReadOnlySet<Type> registeredWriters) =>
        discoveredWriters
            .Where(writer => !registeredWriters.Contains(writer))
            .Distinct()
            .ToList();

    private static bool IsConcreteWriter(Type type) =>
        type is { IsAbstract: false, IsInterface: false } && typeof(IAbwabWriter).IsAssignableFrom(type);

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
