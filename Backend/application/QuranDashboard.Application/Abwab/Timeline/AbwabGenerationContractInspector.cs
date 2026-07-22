using System.Reflection;
using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Timeline;

public static class AbwabGenerationContractInspector
{
    public static IReadOnlyList<Type> DiscoverActionableRequests(IEnumerable<Assembly> assemblies) =>
        assemblies
            .SelectMany(GetLoadableTypes)
            .Where(IsConcreteActionableRequest)
            .Distinct()
            .ToList();

    public static IReadOnlyList<Type> FindRequestsMissingGeneration(IEnumerable<Type> candidateRequestTypes) =>
        candidateRequestTypes
            .Where(type => !typeof(ICarriesExpectedTimelineGeneration).IsAssignableFrom(type))
            .Distinct()
            .ToList();

    private static bool IsConcreteActionableRequest(Type type) =>
        type is { IsAbstract: false, IsInterface: false } &&
        (typeof(IAbwabWriter).IsAssignableFrom(type) || typeof(IAbwabActionableRead).IsAssignableFrom(type));

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
